using Kairn.Application.Common;
using Kairn.Application.Features.AR;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class InvoicePaymentService(AppDbContext db) : IInvoicePaymentService
{
    private const string ArAccountCode = "411000";

    public async Task<IReadOnlyList<InvoicePaymentDto>> GetByInvoiceAsync(
        Guid invoiceId, Guid tenantId, CancellationToken ct = default)
    {
        var payments = await db.InvoicePayments
            .Where(p => p.InvoiceId == invoiceId && p.TenantId == tenantId)
            .OrderBy(p => p.Date)
            .ToListAsync(ct);

        return payments.Select(ToDto).ToList();
    }

    public async Task<Result<InvoicePaymentDto>> RecordAsync(
        RecordPaymentCommand cmd, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == cmd.InvoiceId && i.TenantId == cmd.TenantId, ct);

        if (invoice is null)
            return Result<InvoicePaymentDto>.Fail("Invoice not found.");

        if (invoice.Status is not (InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid or InvoiceStatus.Overdue))
            return Result<InvoicePaymentDto>.Fail("Payments can only be recorded against Sent, Partially Paid, or Overdue invoices.");

        var grandTotal = invoice.Lines.Sum(l => l.LineTotal);
        var alreadyPaid = invoice.Payments.Sum(p => p.Amount);
        var outstanding = grandTotal - alreadyPaid;

        if (cmd.Amount <= 0)
            return Result<InvoicePaymentDto>.Fail("Payment amount must be greater than zero.");
        if (cmd.Amount > outstanding)
            return Result<InvoicePaymentDto>.Fail($"Payment amount ({cmd.Amount:N2}) exceeds the outstanding balance ({outstanding:N2}).");

        var arAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Code == ArAccountCode, ct);
        if (arAccount is null)
            return Result<InvoicePaymentDto>.Fail($"AR account ({ArAccountCode}) not found.");

        var bankAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == cmd.BankAccountId && a.TenantId == cmd.TenantId, ct);
        if (bankAccount is null)
            return Result<InvoicePaymentDto>.Fail("Selected bank/cash account not found.");

        var now = DateTimeOffset.UtcNow;

        // Post journal entry: Dr Bank/Cash, Cr AR
        var entry = new JournalEntry
        {
            TenantId = cmd.TenantId,
            Date = cmd.Date,
            Reference = cmd.Reference ?? $"PMT-{invoice.Reference}",
            Description = $"Payment for invoice {invoice.Reference}",
            CreatedByUserId = cmd.PostedByUserId,
            CreatedByName = cmd.PostedByName,
            IsLocked = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        entry.Lines.Add(new JournalLine
        {
            AccountId = bankAccount.Id,
            Debit = cmd.Amount,
            Credit = 0m,
            Currency = invoice.Currency,
            ExchangeRate = 1m,
            Memo = invoice.Reference,
        });

        entry.Lines.Add(new JournalLine
        {
            AccountId = arAccount.Id,
            Debit = 0m,
            Credit = cmd.Amount,
            Currency = invoice.Currency,
            ExchangeRate = 1m,
            Memo = invoice.Reference,
        });

        db.JournalEntries.Add(entry);

        var payment = new InvoicePayment
        {
            TenantId = cmd.TenantId,
            InvoiceId = cmd.InvoiceId,
            Date = cmd.Date,
            Amount = cmd.Amount,
            Method = cmd.Method,
            Reference = cmd.Reference,
            JournalEntryId = entry.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.InvoicePayments.Add(payment);

        // Update invoice status
        var newPaid = alreadyPaid + cmd.Amount;
        invoice.Status = newPaid >= grandTotal ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        invoice.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return Result<InvoicePaymentDto>.Ok(ToDto(payment));
    }

    public async Task<Result> DeleteAsync(
        Guid paymentId, Guid tenantId, string deletedByUserId, string deletedByName,
        CancellationToken ct = default)
    {
        var payment = await db.InvoicePayments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Lines)
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Payments)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.TenantId == tenantId, ct);

        if (payment is null)
            return Result.Fail("Payment not found.");

        // Post reversing journal entry
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
                    Description = $"Reversal of payment for invoice {payment.Invoice.Reference}",
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

        db.InvoicePayments.Remove(payment);

        // Recalculate invoice status after removal
        var invoice = payment.Invoice;
        var grandTotal = invoice.Lines.Sum(l => l.LineTotal);
        var remainingPaid = invoice.Payments
            .Where(p => p.Id != paymentId)
            .Sum(p => p.Amount);

        invoice.Status = remainingPaid <= 0
            ? (invoice.DueDate < DateOnly.FromDateTime(DateTime.UtcNow) ? InvoiceStatus.Overdue : InvoiceStatus.Sent)
            : (remainingPaid >= grandTotal ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid);
        invoice.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static InvoicePaymentDto ToDto(InvoicePayment p) =>
        new(p.Id, p.InvoiceId, p.Date, p.Amount, p.Method, p.Reference, p.CreatedAt);
}
