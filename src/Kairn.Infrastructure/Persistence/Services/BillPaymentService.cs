using Kairn.Application.Common;
using Kairn.Application.Features.AP;
using Kairn.Application.Features.Tax;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class BillPaymentService(AppDbContext db, ITaxPeriodChecker taxPeriods) : IBillPaymentService
{
    private const string ApAccountCode = "401000";

    public async Task<IReadOnlyList<BillPaymentDto>> GetByBillAsync(
        Guid billId, Guid tenantId, CancellationToken ct = default)
    {
        var payments = await db.BillPayments
            .Where(p => p.BillId == billId && p.TenantId == tenantId)
            .OrderBy(p => p.Date)
            .ToListAsync(ct);

        return payments.Select(ToDto).ToList();
    }

    public async Task<Result<BillPaymentDto>> RecordAsync(
        RecordBillPaymentCommand cmd, CancellationToken ct = default)
    {
        var bill = await db.Bills
            .Include(b => b.Vendor)
            .FirstOrDefaultAsync(b => b.Id == cmd.BillId && b.TenantId == cmd.TenantId, ct);

        if (bill is null)
            return Result<BillPaymentDto>.Fail("Bill not found.");

        if (bill.Status is not (BillStatus.Approved or BillStatus.PartiallyPaid or BillStatus.Overdue))
            return Result<BillPaymentDto>.Fail("Payments can only be recorded against Approved, Partially Paid, or Overdue bills.");

        var outstanding = bill.GrandTotal - bill.AmountPaid;

        if (cmd.Amount <= 0)
            return Result<BillPaymentDto>.Fail("Payment amount must be greater than zero.");
        if (cmd.Amount > outstanding)
            return Result<BillPaymentDto>.Fail($"Payment amount ({cmd.Amount:N2}) exceeds the outstanding balance ({outstanding:N2}).");

        if (await taxPeriods.IsDateLockedAsync(cmd.TenantId, cmd.Date, ct))
            return Result<BillPaymentDto>.Fail("This transaction falls within a locked tax period.");

        var apAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Code == ApAccountCode, ct);
        if (apAccount is null)
            return Result<BillPaymentDto>.Fail($"AP account ({ApAccountCode}) not found.");

        var bankAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == cmd.BankAccountId && a.TenantId == cmd.TenantId, ct);
        if (bankAccount is null)
            return Result<BillPaymentDto>.Fail("Selected bank/cash account not found.");

        var now = DateTimeOffset.UtcNow;

        // Dr AP, Cr Bank/Cash
        var entry = new JournalEntry
        {
            TenantId = cmd.TenantId,
            Date = cmd.Date,
            Reference = cmd.Reference ?? $"PMT-{bill.Reference}",
            Description = $"Payment for bill {bill.Reference} – {bill.Vendor.Name}",
            CreatedByUserId = cmd.PostedByUserId,
            CreatedByName = cmd.PostedByName,
            IsLocked = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        entry.Lines.Add(new JournalLine
        {
            AccountId = apAccount.Id,
            Debit = cmd.Amount,
            Credit = 0m,
            Currency = bill.Currency,
            ExchangeRate = 1m,
            Memo = bill.Reference,
        });

        entry.Lines.Add(new JournalLine
        {
            AccountId = bankAccount.Id,
            Debit = 0m,
            Credit = cmd.Amount,
            Currency = bill.Currency,
            ExchangeRate = 1m,
            Memo = bill.Reference,
        });

        db.JournalEntries.Add(entry);

        var payment = new BillPayment
        {
            TenantId = cmd.TenantId,
            BillId = cmd.BillId,
            Date = cmd.Date,
            Amount = cmd.Amount,
            Method = cmd.Method,
            Reference = cmd.Reference,
            JournalEntryId = entry.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.BillPayments.Add(payment);

        bill.AmountPaid += cmd.Amount;
        bill.Status = bill.AmountPaid >= bill.GrandTotal ? BillStatus.Paid : BillStatus.PartiallyPaid;
        bill.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return Result<BillPaymentDto>.Ok(ToDto(payment));
    }

    public async Task<Result> DeleteAsync(
        Guid paymentId, Guid tenantId, string deletedByUserId, string deletedByName,
        CancellationToken ct = default)
    {
        var payment = await db.BillPayments
            .Include(p => p.Bill)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.TenantId == tenantId, ct);

        if (payment is null)
            return Result.Fail("Payment not found.");

        if (await taxPeriods.IsDateLockedAsync(tenantId, payment.Date, ct))
            return Result.Fail("This transaction falls within a locked tax period.");

        if (payment.JournalEntryId.HasValue)
        {
            var original = await db.JournalEntries
                .Include(e => e.Lines)
                .FirstOrDefaultAsync(e => e.Id == payment.JournalEntryId.Value, ct);

            if (original is not null)
            {
                var now = DateTimeOffset.UtcNow;
                var reversal = new JournalEntry
                {
                    TenantId = tenantId,
                    Date = DateOnly.FromDateTime(now.UtcDateTime),
                    Reference = $"REV-{original.Reference}",
                    Description = $"Reversal of payment for bill {payment.Bill.Reference}",
                    CreatedByUserId = deletedByUserId,
                    CreatedByName = deletedByName,
                    IsLocked = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                foreach (var line in original.Lines)
                {
                    reversal.Lines.Add(new JournalLine
                    {
                        AccountId = line.AccountId,
                        Debit = line.Credit,
                        Credit = line.Debit,
                        Currency = line.Currency,
                        ExchangeRate = line.ExchangeRate,
                        Memo = $"Reversal – {line.Memo}",
                    });
                }

                db.JournalEntries.Add(reversal);
            }
        }

        db.BillPayments.Remove(payment);

        var bill = payment.Bill;
        bill.AmountPaid = Math.Max(0m, bill.AmountPaid - payment.Amount);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        bill.Status = bill.AmountPaid <= 0
            ? (bill.DueDate < today ? BillStatus.Overdue : BillStatus.Approved)
            : (bill.AmountPaid >= bill.GrandTotal ? BillStatus.Paid : BillStatus.PartiallyPaid);
        bill.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static BillPaymentDto ToDto(BillPayment p) =>
        new(p.Id, p.BillId, p.Date, p.Amount, p.Method, p.Reference, p.CreatedAt);
}
