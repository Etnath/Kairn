using Kairn.Application.Common;
using Kairn.Application.Features.AP;
using Kairn.Application.Features.Tax;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class BillService(AppDbContext db, ITaxPeriodChecker taxPeriods) : IBillService
{
    private const string ApAccountCode = "401000";
    private const string InputVatAccountCode = "445660";

    public async Task<PagedResult<BillDto>> GetPagedAsync(BillQuery query, CancellationToken ct = default)
    {
        await MarkOverdueAsync(query.TenantId, ct);

        var q = db.Bills
            .Include(b => b.Vendor)
            .Include(b => b.Lines).ThenInclude(l => l.ExpenseAccount)
            .Where(b => b.TenantId == query.TenantId);

        if (query.VendorId.HasValue)
            q = q.Where(b => b.VendorId == query.VendorId.Value);
        if (query.Status.HasValue)
            q = q.Where(b => b.Status == query.Status.Value);
        else if (query.ExcludeClosedStatuses)
            q = q.Where(b => b.Status != BillStatus.Void && b.Status != BillStatus.Paid);

        if (query.DueDateFrom.HasValue)
            q = q.Where(b => b.DueDate >= query.DueDateFrom.Value);
        if (query.DueDateTo.HasValue)
            q = q.Where(b => b.DueDate <= query.DueDateTo.Value);

        q = query.SortBy?.ToLowerInvariant() switch
        {
            "amount" => query.SortDescending
                ? q.OrderByDescending(b => b.GrandTotal).ThenByDescending(b => b.Date)
                : q.OrderBy(b => b.GrandTotal).ThenByDescending(b => b.Date),
            "duedate" => query.SortDescending
                ? q.OrderByDescending(b => b.DueDate).ThenByDescending(b => b.Date)
                : q.OrderBy(b => b.DueDate).ThenByDescending(b => b.Date),
            _ => query.SortDescending
                ? q.OrderByDescending(b => b.Date).ThenByDescending(b => b.Reference)
                : q.OrderBy(b => b.Date).ThenBy(b => b.Reference),
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var billIds = items.Select(b => b.Id).ToList();
        var attachedBillIds = await db.BillAttachments
            .Where(a => billIds.Contains(a.BillId))
            .Select(a => a.BillId)
            .Distinct()
            .ToListAsync(ct);
        var attachedSet = attachedBillIds.ToHashSet();

        return new PagedResult<BillDto>(
            items.Select(b => ToDto(b, attachedSet.Contains(b.Id))).ToList(),
            total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<BillDto>> GetByVendorAsync(Guid vendorId, Guid tenantId, CancellationToken ct = default)
    {
        var bills = await db.Bills
            .Include(b => b.Vendor)
            .Include(b => b.Lines).ThenInclude(l => l.ExpenseAccount)
            .Where(b => b.TenantId == tenantId && b.VendorId == vendorId)
            .OrderByDescending(b => b.Date)
            .ThenByDescending(b => b.Reference)
            .ToListAsync(ct);

        var billIds = bills.Select(b => b.Id).ToList();
        var attachedBillIds = await db.BillAttachments
            .Where(a => billIds.Contains(a.BillId))
            .Select(a => a.BillId)
            .Distinct()
            .ToListAsync(ct);
        var attachedSet = attachedBillIds.ToHashSet();

        return bills.Select(b => ToDto(b, attachedSet.Contains(b.Id))).ToList();
    }

    public async Task<BillDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var bill = await db.Bills
            .Include(b => b.Vendor)
            .Include(b => b.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId, ct);

        if (bill is null) return null;

        var hasAttachment = await db.BillAttachments.AnyAsync(a => a.BillId == id, ct);
        return ToDto(bill, hasAttachment);
    }

    public async Task<Result<BillDto>> CreateAsync(CreateBillCommand cmd, CancellationToken ct = default)
    {
        if (!cmd.Lines.Any())
            return Result<BillDto>.Fail("A bill must have at least one line.");

        if (await taxPeriods.IsDateLockedAsync(cmd.TenantId, cmd.Date, ct))
            return Result<BillDto>.Fail("This transaction falls within a locked tax period.");

        var now = DateTimeOffset.UtcNow;
        var bill = new Bill
        {
            TenantId = cmd.TenantId,
            VendorId = cmd.VendorId,
            Reference = cmd.Reference,
            Date = cmd.Date,
            DueDate = cmd.DueDate,
            Currency = cmd.Currency,
            Notes = cmd.Notes,
            Status = BillStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var (l, i) in cmd.Lines.Select((l, i) => (l, i)))
        {
            bill.Lines.Add(new BillLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                TaxRate          = l.TaxRate,
                TaxRateId        = l.TaxRateId,
                ExpenseAccountId = l.ExpenseAccountId,
                SortOrder = l.SortOrder == 0 ? i : l.SortOrder,
            });
        }

        bill.GrandTotal = bill.Lines.Sum(l => l.LineTotal);

        var apSettings = await db.TenantApSettings.FindAsync([cmd.TenantId], ct);
        if (apSettings?.ApprovalEnabled == true && bill.GrandTotal > apSettings.ApprovalThreshold)
            bill.Status = BillStatus.PendingApproval;

        if (cmd.AttachmentData is { Length: > 0 } && cmd.AttachmentFileName is not null)
        {
            bill.Attachments.Add(new BillAttachment
            {
                FileName = cmd.AttachmentFileName,
                ContentType = cmd.AttachmentContentType ?? "application/octet-stream",
                Data = cmd.AttachmentData,
                UploadedAt = now,
            });
        }

        db.Bills.Add(bill);
        await db.SaveChangesAsync(ct);

        var saved = await db.Bills
            .Include(b => b.Vendor)
            .Include(b => b.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstAsync(b => b.Id == bill.Id, ct);

        var hasAttachment = await db.BillAttachments.AnyAsync(a => a.BillId == bill.Id, ct);
        return Result<BillDto>.Ok(ToDto(saved, hasAttachment));
    }

    public async Task<Result<BillDto>> UpdateAsync(UpdateBillCommand cmd, CancellationToken ct = default)
    {
        var bill = await db.Bills
            .Include(b => b.Lines)
            .Include(b => b.Attachments)
            .FirstOrDefaultAsync(b => b.Id == cmd.Id && b.TenantId == cmd.TenantId, ct);

        if (bill is null)
            return Result<BillDto>.Fail("Bill not found.");
        if (bill.Status is not (BillStatus.Draft or BillStatus.Rejected))
            return Result<BillDto>.Fail("Only draft or rejected bills can be edited.");
        if (!cmd.Lines.Any())
            return Result<BillDto>.Fail("A bill must have at least one line.");

        if (await taxPeriods.IsDateLockedAsync(cmd.TenantId, cmd.Date, ct))
            return Result<BillDto>.Fail("This transaction falls within a locked tax period.");

        bill.VendorId = cmd.VendorId;
        bill.Reference = cmd.Reference;
        bill.Date = cmd.Date;
        bill.DueDate = cmd.DueDate;
        bill.Currency = cmd.Currency;
        bill.Notes = cmd.Notes;
        bill.UpdatedAt = DateTimeOffset.UtcNow;

        db.BillLines.RemoveRange(bill.Lines);

        foreach (var (l, i) in cmd.Lines.Select((l, i) => (l, i)))
        {
            bill.Lines.Add(new BillLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                TaxRate          = l.TaxRate,
                TaxRateId        = l.TaxRateId,
                ExpenseAccountId = l.ExpenseAccountId,
                SortOrder = l.SortOrder == 0 ? i : l.SortOrder,
            });
        }

        bill.GrandTotal = bill.Lines.Sum(l => l.LineTotal);

        // Resubmitting a rejected bill: re-apply threshold check
        if (bill.Status == BillStatus.Rejected)
        {
            bill.RejectionReason = null;
            var apSettings = await db.TenantApSettings.FindAsync([cmd.TenantId], ct);
            bill.Status = apSettings?.ApprovalEnabled == true && bill.GrandTotal > apSettings.ApprovalThreshold
                ? BillStatus.PendingApproval
                : BillStatus.Draft;
        }

        if (cmd.RemoveAttachment)
            db.BillAttachments.RemoveRange(bill.Attachments);

        if (cmd.AttachmentData is { Length: > 0 } && cmd.AttachmentFileName is not null)
        {
            db.BillAttachments.RemoveRange(bill.Attachments);
            bill.Attachments.Add(new BillAttachment
            {
                FileName = cmd.AttachmentFileName,
                ContentType = cmd.AttachmentContentType ?? "application/octet-stream",
                Data = cmd.AttachmentData,
                UploadedAt = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);

        var saved = await db.Bills
            .Include(b => b.Vendor)
            .Include(b => b.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstAsync(b => b.Id == bill.Id, ct);

        var hasAttachment = await db.BillAttachments.AnyAsync(a => a.BillId == bill.Id, ct);
        return Result<BillDto>.Ok(ToDto(saved, hasAttachment));
    }

    public async Task<Result<BillDto>> ApproveAsync(ApproveBillCommand cmd, CancellationToken ct = default)
    {
        var bill = await db.Bills
            .Include(b => b.Vendor)
            .Include(b => b.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstOrDefaultAsync(b => b.Id == cmd.Id && b.TenantId == cmd.TenantId, ct);

        if (bill is null)
            return Result<BillDto>.Fail("Bill not found.");
        if (bill.Status is not (BillStatus.Draft or BillStatus.PendingApproval))
            return Result<BillDto>.Fail("Only Draft or Pending Approval bills can be approved.");
        if (!bill.Lines.Any())
            return Result<BillDto>.Fail("Cannot approve a bill with no lines.");

        if (await taxPeriods.IsDateLockedAsync(cmd.TenantId, bill.Date, ct))
            return Result<BillDto>.Fail("This transaction falls within a locked tax period.");

        var apAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Code == ApAccountCode, ct);
        if (apAccount is null)
            return Result<BillDto>.Fail($"AP account ({ApAccountCode}) not found. Please set up the chart of accounts.");

        var subtotal = bill.Lines.Sum(l => l.NetAmount);
        var totalTax = bill.Lines.Sum(l => l.TaxAmount);
        var grandTotal = subtotal + totalTax;

        var now = DateTimeOffset.UtcNow;
        var entry = new JournalEntry
        {
            TenantId = cmd.TenantId,
            Date = bill.Date,
            Reference = bill.Reference,
            Description = $"Bill {bill.Reference} – {bill.Vendor.Name}",
            CreatedByUserId = cmd.PostedByUserId,
            CreatedByName = cmd.PostedByName,
            IsLocked = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Cr AP (full amount)
        entry.Lines.Add(new JournalLine
        {
            AccountId = apAccount.Id,
            Debit = 0m,
            Credit = grandTotal,
            Currency = bill.Currency,
            ExchangeRate = 1m,
            Memo = bill.Reference,
        });

        // Dr Expense accounts (grouped by account)
        var expenseGroups = bill.Lines
            .GroupBy(l => l.ExpenseAccountId)
            .Select(g => new { AccountId = g.Key, Amount = g.Sum(l => l.NetAmount) });

        foreach (var group in expenseGroups)
        {
            entry.Lines.Add(new JournalLine
            {
                AccountId = group.AccountId,
                Debit = group.Amount,
                Credit = 0m,
                Currency = bill.Currency,
                ExchangeRate = 1m,
                Memo = bill.Reference,
            });
        }

        // Dr Input VAT (if any tax)
        if (totalTax > 0)
        {
            var vatAccount = await db.Accounts
                .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Code == InputVatAccountCode, ct);
            if (vatAccount is not null)
            {
                entry.Lines.Add(new JournalLine
                {
                    AccountId = vatAccount.Id,
                    Debit = totalTax,
                    Credit = 0m,
                    Currency = bill.Currency,
                    ExchangeRate = 1m,
                    Memo = bill.Reference,
                });
            }
        }

        db.JournalEntries.Add(entry);

        bill.Status = BillStatus.Approved;
        bill.GrandTotal = grandTotal;
        bill.JournalEntryId = entry.Id;
        bill.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        var hasAttachment = await db.BillAttachments.AnyAsync(a => a.BillId == bill.Id, ct);
        return Result<BillDto>.Ok(ToDto(bill, hasAttachment));
    }

    public async Task<Result<BillDto>> RejectAsync(RejectBillCommand cmd, CancellationToken ct = default)
    {
        var bill = await db.Bills
            .Include(b => b.Vendor)
            .Include(b => b.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstOrDefaultAsync(b => b.Id == cmd.Id && b.TenantId == cmd.TenantId, ct);

        if (bill is null) return Result<BillDto>.Fail("Bill not found.");
        if (bill.Status != BillStatus.PendingApproval)
            return Result<BillDto>.Fail("Only Pending Approval bills can be rejected.");

        bill.Status = BillStatus.Rejected;
        bill.RejectionReason = cmd.RejectionReason;
        bill.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        var hasAttachment = await db.BillAttachments.AnyAsync(a => a.BillId == bill.Id, ct);
        return Result<BillDto>.Ok(ToDto(bill, hasAttachment));
    }

    public async Task<int> GetPendingApprovalCountAsync(Guid tenantId, CancellationToken ct = default)
        => await db.Bills.CountAsync(b => b.TenantId == tenantId && b.Status == BillStatus.PendingApproval, ct);

    public async Task<Result> VoidAsync(VoidBillCommand cmd, CancellationToken ct = default)
    {
        var bill = await db.Bills
            .Include(b => b.Vendor)
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == cmd.Id && b.TenantId == cmd.TenantId, ct);

        if (bill is null)
            return Result.Fail("Bill not found.");
        if (bill.Status == BillStatus.Void)
            return Result.Fail("Bill is already voided.");
        if (bill.Status == BillStatus.Paid)
            return Result.Fail("Paid bills cannot be voided.");
        if (bill.Status == BillStatus.PartiallyPaid)
            return Result.Fail("Partially paid bills cannot be voided.");

        if (await taxPeriods.IsDateLockedAsync(cmd.TenantId, bill.Date, ct))
            return Result.Fail("This transaction falls within a locked tax period.");

        var now = DateTimeOffset.UtcNow;

        if (bill.JournalEntryId.HasValue)
        {
            var original = await db.JournalEntries
                .Include(e => e.Lines)
                .FirstOrDefaultAsync(e => e.Id == bill.JournalEntryId.Value, ct);

            if (original is not null)
            {
                var reversal = new JournalEntry
                {
                    TenantId = cmd.TenantId,
                    Date = DateOnly.FromDateTime(now.UtcDateTime),
                    Reference = $"VOID-{bill.Reference}",
                    Description = $"Void of bill {bill.Reference} – {bill.Vendor.Name}",
                    CreatedByUserId = cmd.PostedByUserId,
                    CreatedByName = cmd.PostedByName,
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
                        Memo = $"VOID-{line.Memo}",
                    });
                }

                db.JournalEntries.Add(reversal);
            }
        }

        bill.Status = BillStatus.Void;
        bill.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task MarkAllOverdueAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdue = await db.Bills
            .Where(b => (b.Status == BillStatus.Approved || b.Status == BillStatus.PartiallyPaid)
                        && b.DueDate < today)
            .ToListAsync(ct);

        if (!overdue.Any()) return;

        foreach (var bill in overdue)
        {
            bill.Status = BillStatus.Overdue;
            bill.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task MarkOverdueAsync(Guid tenantId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdue = await db.Bills
            .Where(b => b.TenantId == tenantId
                        && (b.Status == BillStatus.Approved || b.Status == BillStatus.PartiallyPaid)
                        && b.DueDate < today)
            .ToListAsync(ct);

        if (!overdue.Any()) return;
        foreach (var b in overdue)
        {
            b.Status = BillStatus.Overdue;
            b.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BillDto>> GetUpcomingAsync(Guid tenantId, CancellationToken ct = default)
    {
        await MarkOverdueAsync(tenantId, ct);

        var bills = await db.Bills
            .Include(b => b.Vendor)
            .Include(b => b.Lines).ThenInclude(l => l.ExpenseAccount)
            .Where(b => b.TenantId == tenantId
                        && (b.Status == BillStatus.Approved
                            || b.Status == BillStatus.PartiallyPaid
                            || b.Status == BillStatus.Overdue))
            .OrderBy(b => b.DueDate)
            .ThenBy(b => b.Vendor.Name)
            .ToListAsync(ct);

        var billIds = bills.Select(b => b.Id).ToList();
        var attachedSet = (await db.BillAttachments
            .Where(a => billIds.Contains(a.BillId))
            .Select(a => a.BillId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        return bills.Select(b => ToDto(b, attachedSet.Contains(b.Id))).ToList();
    }

    public async Task<(int Count, decimal TotalOutstanding)> GetBillsDueSoonSummaryAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(7);

        var bills = await db.Bills
            .Include(b => b.Lines)
            .Where(b => b.TenantId == tenantId
                        && (b.Status == BillStatus.Approved || b.Status == BillStatus.PartiallyPaid)
                        && b.DueDate >= today
                        && b.DueDate <= cutoff)
            .ToListAsync(ct);

        var count = bills.Count;
        var total = bills.Sum(b => b.GrandTotal - b.AmountPaid);
        return (count, total);
    }

    public async Task<(byte[] Data, string FileName, string ContentType)?> DownloadAttachmentAsync(
        Guid billId, Guid attachmentId, Guid tenantId, CancellationToken ct = default)
    {
        var exists = await db.Bills
            .AnyAsync(b => b.Id == billId && b.TenantId == tenantId, ct);
        if (!exists) return null;

        var q = db.BillAttachments.Where(a => a.BillId == billId);
        if (attachmentId != Guid.Empty)
            q = q.Where(a => a.Id == attachmentId);
        var attachment = await q.FirstOrDefaultAsync(ct);

        return attachment is null ? null : (attachment.Data, attachment.FileName, attachment.ContentType);
    }

    private static BillDto ToDto(Bill b, bool hasAttachment)
    {
        var lines = b.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new BillLineDto(
                l.Id, l.Description, l.Quantity, l.UnitPrice, l.TaxRate,
                l.ExpenseAccountId, l.ExpenseAccount?.Name ?? "", l.SortOrder, l.TaxRateId))
            .ToList();

        var subtotal = b.Lines.Sum(l => l.NetAmount);
        var totalTax = b.Lines.Sum(l => l.TaxAmount);
        var grandTotal = subtotal + totalTax;

        return new BillDto(
            b.Id, b.TenantId, b.VendorId, b.Vendor?.Name ?? "",
            b.Reference, b.Date, b.DueDate, b.Status, b.Currency, b.Notes,
            subtotal, totalTax, grandTotal, b.AmountPaid, lines, hasAttachment,
            b.RejectionReason);
    }
}
