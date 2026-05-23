using Kairn.Application.Common;
using Kairn.Application.Features.AR;
using Kairn.Domain.Entities;
using Kairn.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class InvoiceService(AppDbContext db) : IInvoiceService
{
    // Well-known account codes for GL posting
    private const string ArAccountCode = "411000";
    private const string VatAccountCode = "445710";

    public async Task<PagedResult<InvoiceDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default)
    {
        await MarkOverdueAsync(query.TenantId, ct);

        var q = db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes).ThenInclude(cn => cn.Lines)
            .Include(i => i.OriginalInvoice)
            .Where(i => i.TenantId == query.TenantId);

        if (query.CustomerId.HasValue)
            q = q.Where(i => i.CustomerId == query.CustomerId.Value);
        if (query.Status.HasValue)
            q = q.Where(i => i.Status == query.Status.Value);
        else if (query.ExcludeClosedStatuses)
            q = q.Where(i => i.Status != InvoiceStatus.Void && i.Status != InvoiceStatus.Paid);
        if (query.From.HasValue)
            q = q.Where(i => i.Date >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(i => i.Date <= query.To.Value);

        q = q.OrderByDescending(i => i.Date).ThenByDescending(i => i.Reference);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<InvoiceDto>(items.Select(ToDto).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes).ThenInclude(cn => cn.Lines)
            .Include(i => i.OriginalInvoice)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, ct);
        return invoice is null ? null : ToDto(invoice);
    }

    public async Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceCommand cmd, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var invoice = new Invoice
        {
            TenantId = cmd.TenantId,
            CustomerId = cmd.CustomerId,
            Reference = cmd.Reference,
            Date = cmd.Date,
            DueDate = cmd.DueDate,
            Currency = cmd.Currency,
            Notes = cmd.Notes,
            RevenueAccountId = cmd.RevenueAccountId,
            Status = InvoiceStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var (l, i) in cmd.Lines.Select((l, i) => (l, i)))
        {
            invoice.Lines.Add(new InvoiceLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPct = l.DiscountPct,
                TaxRate   = l.TaxRate,
                TaxRateId = l.TaxRateId,
                SortOrder = l.SortOrder == 0 ? i : l.SortOrder,
            });
        }

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        // Reload with Customer
        var saved = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoice.Id, ct);
        return Result<InvoiceDto>.Ok(ToDto(saved));
    }

    public async Task<Result<InvoiceDto>> UpdateAsync(UpdateInvoiceCommand cmd, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == cmd.Id && i.TenantId == cmd.TenantId, ct);

        if (invoice is null)
            return Result<InvoiceDto>.Fail("Invoice not found.");
        if (invoice.Status != InvoiceStatus.Draft)
            return Result<InvoiceDto>.Fail("Only draft invoices can be edited.");

        invoice.CustomerId = cmd.CustomerId;
        invoice.Reference = cmd.Reference;
        invoice.Date = cmd.Date;
        invoice.DueDate = cmd.DueDate;
        invoice.Currency = cmd.Currency;
        invoice.Notes = cmd.Notes;
        invoice.RevenueAccountId = cmd.RevenueAccountId;
        invoice.UpdatedAt = DateTimeOffset.UtcNow;

        db.InvoiceLines.RemoveRange(invoice.Lines);
        invoice.Lines.Clear();

        foreach (var (l, i) in cmd.Lines.Select((l, i) => (l, i)))
        {
            invoice.Lines.Add(new InvoiceLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPct = l.DiscountPct,
                TaxRate   = l.TaxRate,
                TaxRateId = l.TaxRateId,
                SortOrder = l.SortOrder == 0 ? i : l.SortOrder,
            });
        }

        await db.SaveChangesAsync(ct);

        var saved = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoice.Id, ct);
        return Result<InvoiceDto>.Ok(ToDto(saved));
    }

    public async Task<Result<InvoiceDto>> SendAsync(SendInvoiceCommand cmd, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == cmd.Id && i.TenantId == cmd.TenantId, ct);

        if (invoice is null)
            return Result<InvoiceDto>.Fail("Invoice not found.");
        if (invoice.Status != InvoiceStatus.Draft)
            return Result<InvoiceDto>.Fail("Only draft invoices can be sent.");
        if (!invoice.Lines.Any())
            return Result<InvoiceDto>.Fail("Cannot send an invoice with no lines.");

        var arAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Code == ArAccountCode, ct);
        if (arAccount is null)
            return Result<InvoiceDto>.Fail($"AR account ({ArAccountCode}) not found. Please set up the chart of accounts.");

        var revenueAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == cmd.RevenueAccountId && a.TenantId == cmd.TenantId, ct);
        if (revenueAccount is null)
            return Result<InvoiceDto>.Fail("Selected revenue account not found.");

        var subtotal = invoice.Lines.Sum(l => l.NetAmount);
        var totalTax = invoice.Lines.Sum(l => l.TaxAmount);
        var grandTotal = subtotal + totalTax;

        var now = DateTimeOffset.UtcNow;
        var entry = new JournalEntry
        {
            TenantId = cmd.TenantId,
            Date = invoice.Date,
            Reference = invoice.Reference,
            Description = $"Invoice {invoice.Reference} – {invoice.Customer.Name}",
            CreatedByUserId = cmd.PostedByUserId,
            CreatedByName = cmd.PostedByName,
            IsLocked = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Dr AR (full amount)
        entry.Lines.Add(new JournalLine
        {
            AccountId = arAccount.Id,
            Debit = grandTotal,
            Credit = 0m,
            Currency = invoice.Currency,
            ExchangeRate = 1m,
            Memo = invoice.Reference,
        });

        // Cr Revenue (net amount)
        entry.Lines.Add(new JournalLine
        {
            AccountId = revenueAccount.Id,
            Debit = 0m,
            Credit = subtotal,
            Currency = invoice.Currency,
            ExchangeRate = 1m,
            Memo = invoice.Reference,
        });

        // Cr VAT Payable (if any tax)
        if (totalTax > 0)
        {
            var vatAccount = await db.Accounts
                .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Code == VatAccountCode, ct);
            if (vatAccount is not null)
            {
                entry.Lines.Add(new JournalLine
                {
                    AccountId = vatAccount.Id,
                    Debit = 0m,
                    Credit = totalTax,
                    Currency = invoice.Currency,
                    ExchangeRate = 1m,
                    Memo = invoice.Reference,
                });
            }
        }

        db.JournalEntries.Add(entry);

        invoice.Status = InvoiceStatus.Sent;
        invoice.JournalEntryId = entry.Id;
        invoice.RevenueAccountId = cmd.RevenueAccountId;
        invoice.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return Result<InvoiceDto>.Ok(ToDto(invoice));
    }

    public async Task<Result> VoidAsync(VoidInvoiceCommand cmd, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == cmd.Id && i.TenantId == cmd.TenantId, ct);

        if (invoice is null)
            return Result.Fail("Invoice not found.");
        if (invoice.Status == InvoiceStatus.Void)
            return Result.Fail("Invoice is already voided.");
        if (invoice.Status == InvoiceStatus.Paid)
            return Result.Fail("Paid invoices cannot be voided.");

        var now = DateTimeOffset.UtcNow;

        // Post reversing journal entry if one exists
        if (invoice.JournalEntryId.HasValue)
        {
            var original = await db.JournalEntries
                .Include(e => e.Lines)
                .FirstOrDefaultAsync(e => e.Id == invoice.JournalEntryId.Value, ct);

            if (original is not null)
            {
                var reversal = new JournalEntry
                {
                    TenantId = cmd.TenantId,
                    Date = DateOnly.FromDateTime(now.UtcDateTime),
                    Reference = $"VOID-{invoice.Reference}",
                    Description = $"Void of invoice {invoice.Reference} – {invoice.Customer.Name}",
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
                        Memo = $"Reversal – {line.Memo}",
                    });
                }

                db.JournalEntries.Add(reversal);
            }
        }

        invoice.Status = InvoiceStatus.Void;
        invoice.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<byte[]?> GeneratePdfAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes).ThenInclude(cn => cn.Lines)
            .Include(i => i.OriginalInvoice)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, ct);

        if (invoice is null) return null;
        return InvoicePdfGenerator.Generate(ToDto(invoice));
    }

    public async Task<string> GenerateReferenceAsync(Guid tenantId, DateOnly date, CancellationToken ct = default)
    {
        var count = await db.Invoices
            .Where(i => i.TenantId == tenantId)
            .CountAsync(ct);
        return $"INV-{date:yyyyMM}-{(count + 1):D4}";
    }

    private static InvoiceDto ToDto(Invoice i)
    {
        var lines = i.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new InvoiceLineDto(l.Id, l.Description, l.Quantity, l.UnitPrice,
                l.DiscountPct, l.TaxRate, l.SortOrder, l.TaxRateId))
            .ToList();

        var subtotal = i.Lines.Sum(l => l.NetAmount);
        var totalDiscount = i.Lines.Sum(l => l.DiscountAmount);
        var totalTax = i.Lines.Sum(l => l.TaxAmount);
        var amountPaid = i.Payments.Sum(p => p.Amount)
                       + i.CreditNotes.Sum(cn => cn.Lines.Sum(l => l.LineTotal));

        return new InvoiceDto(
            i.Id,
            i.CustomerId,
            i.Customer?.Name ?? "",
            i.Customer?.Email,
            i.Customer?.Address,
            i.Customer?.TaxNumber,
            i.Reference,
            i.Date,
            i.DueDate,
            i.Status,
            i.Currency,
            i.Notes,
            i.RevenueAccountId,
            subtotal,
            totalDiscount,
            totalTax,
            subtotal + totalTax,
            amountPaid,
            lines,
            i.IsCreditNote,
            i.OriginalInvoiceId,
            i.OriginalInvoice?.Reference);
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetCreditNotesAsync(
        Guid originalInvoiceId, Guid tenantId, CancellationToken ct = default)
    {
        var items = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes).ThenInclude(cn => cn.Lines)
            .Include(i => i.OriginalInvoice)
            .Where(i => i.TenantId == tenantId
                     && i.IsCreditNote
                     && i.OriginalInvoiceId == originalInvoiceId)
            .OrderBy(i => i.Date)
            .ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<Result<InvoiceDto>> IssueCreditNoteAsync(IssueCreditNoteCommand cmd, CancellationToken ct = default)
    {
        var original = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes).ThenInclude(cn => cn.Lines)
            .FirstOrDefaultAsync(i => i.Id == cmd.OriginalInvoiceId && i.TenantId == cmd.TenantId, ct);

        if (original is null)
            return Result<InvoiceDto>.Fail("Invoice not found.");
        if (original.IsCreditNote)
            return Result<InvoiceDto>.Fail("Cannot issue a credit note against a credit note.");
        if (original.Status is not (InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid
                                    or InvoiceStatus.Paid or InvoiceStatus.Overdue))
            return Result<InvoiceDto>.Fail("Credit notes can only be issued for Sent, Partially Paid, Paid, or Overdue invoices.");

        var originalGrandTotal = original.Lines.Sum(l => l.LineTotal);

        var cnSubtotal = cmd.Lines.Sum(l =>
        {
            var gross = l.Quantity * l.UnitPrice;
            return gross - gross * l.DiscountPct / 100m;
        });
        var cnTax = cmd.Lines.Sum(l =>
        {
            var gross = l.Quantity * l.UnitPrice;
            var net = gross * (1 - l.DiscountPct / 100m);
            return net * l.TaxRate / 100m;
        });
        var cnTotal = cnSubtotal + cnTax;

        if (cnTotal <= 0)
            return Result<InvoiceDto>.Fail("Credit note amount must be positive.");
        if (cnTotal > originalGrandTotal)
            return Result<InvoiceDto>.Fail(
                $"Credit note amount ({original.Currency} {cnTotal:N2}) cannot exceed the original invoice total ({original.Currency} {originalGrandTotal:N2}).");

        var cnCount = await db.Invoices
            .Where(i => i.TenantId == cmd.TenantId && i.IsCreditNote)
            .CountAsync(ct);
        var reference = $"CN-{cmd.Date:yyyyMM}-{(cnCount + 1):D4}";

        var now = DateTimeOffset.UtcNow;
        var creditNote = new Invoice
        {
            TenantId = cmd.TenantId,
            CustomerId = original.CustomerId,
            Reference = reference,
            Date = cmd.Date,
            DueDate = cmd.Date,
            Currency = original.Currency,
            Status = InvoiceStatus.Sent,
            Notes = cmd.Notes,
            RevenueAccountId = cmd.RevenueAccountId ?? original.RevenueAccountId,
            IsCreditNote = true,
            OriginalInvoiceId = original.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var (l, idx) in cmd.Lines.Select((l, i) => (l, i)))
        {
            creditNote.Lines.Add(new InvoiceLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPct = l.DiscountPct,
                TaxRate = l.TaxRate,
                SortOrder = l.SortOrder == 0 ? idx : l.SortOrder,
            });
        }

        var arAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Code == ArAccountCode, ct);
        if (arAccount is null)
            return Result<InvoiceDto>.Fail($"AR account ({ArAccountCode}) not found.");

        var revenueAccountId = cmd.RevenueAccountId ?? original.RevenueAccountId;
        if (revenueAccountId is null)
            return Result<InvoiceDto>.Fail("Revenue account not found. Please specify a revenue account.");
        var revenueAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == revenueAccountId.Value && a.TenantId == cmd.TenantId, ct);
        if (revenueAccount is null)
            return Result<InvoiceDto>.Fail("Revenue account not found.");

        var glEntry = new JournalEntry
        {
            TenantId = cmd.TenantId,
            Date = cmd.Date,
            Reference = reference,
            Description = $"Credit note {reference} – {original.Customer.Name}",
            CreatedByUserId = cmd.PostedByUserId,
            CreatedByName = cmd.PostedByName,
            IsLocked = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Cr AR (full CN amount)
        glEntry.Lines.Add(new JournalLine
        {
            AccountId = arAccount.Id,
            Debit = 0m,
            Credit = cnTotal,
            Currency = original.Currency,
            ExchangeRate = 1m,
            Memo = reference,
        });

        // Dr Revenue (net amount)
        glEntry.Lines.Add(new JournalLine
        {
            AccountId = revenueAccount.Id,
            Debit = cnSubtotal,
            Credit = 0m,
            Currency = original.Currency,
            ExchangeRate = 1m,
            Memo = reference,
        });

        // Dr VAT Recoverable (if any tax)
        if (cnTax > 0)
        {
            var vatAccount = await db.Accounts
                .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Code == VatAccountCode, ct);
            if (vatAccount is not null)
            {
                glEntry.Lines.Add(new JournalLine
                {
                    AccountId = vatAccount.Id,
                    Debit = cnTax,
                    Credit = 0m,
                    Currency = original.Currency,
                    ExchangeRate = 1m,
                    Memo = reference,
                });
            }
        }

        db.JournalEntries.Add(glEntry);
        creditNote.JournalEntryId = glEntry.Id;
        db.Invoices.Add(creditNote);

        // Update original invoice status
        var existingApplied = original.Payments.Sum(p => p.Amount)
                            + original.CreditNotes.Sum(cn => cn.Lines.Sum(l => l.LineTotal));
        var newApplied = existingApplied + cnTotal;
        var newOutstanding = originalGrandTotal - newApplied;

        if (newOutstanding <= 0)
            original.Status = InvoiceStatus.Paid;
        else if (newApplied > 0)
            original.Status = InvoiceStatus.PartiallyPaid;
        original.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        var saved = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes).ThenInclude(cn => cn.Lines)
            .Include(i => i.OriginalInvoice)
            .FirstAsync(i => i.Id == creditNote.Id, ct);

        return Result<InvoiceDto>.Ok(ToDto(saved));
    }

    public async Task MarkAllOverdueAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdueInvoices = await db.Invoices
            .Where(i => !i.IsCreditNote
                     && i.DueDate < today
                     && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.PartiallyPaid))
            .ToListAsync(ct);

        if (overdueInvoices.Count > 0)
        {
            foreach (var inv in overdueInvoices)
                inv.Status = InvoiceStatus.Overdue;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<(int Count, decimal TotalOutstanding)> GetOverdueSummaryAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var invoices = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes).ThenInclude(cn => cn.Lines)
            .Where(i => i.TenantId == tenantId && !i.IsCreditNote && i.Status == InvoiceStatus.Overdue)
            .ToListAsync(ct);

        var total = invoices.Sum(i =>
            i.Lines.Sum(l => l.LineTotal)
            - i.Payments.Sum(p => p.Amount)
            - i.CreditNotes.Sum(cn => cn.Lines.Sum(l => l.LineTotal)));
        return (invoices.Count, total);
    }

    private async Task MarkOverdueAsync(Guid tenantId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdueInvoices = await db.Invoices
            .Where(i => i.TenantId == tenantId
                     && !i.IsCreditNote
                     && i.DueDate < today
                     && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.PartiallyPaid))
            .ToListAsync(ct);

        if (overdueInvoices.Count > 0)
        {
            foreach (var inv in overdueInvoices)
                inv.Status = InvoiceStatus.Overdue;
            await db.SaveChangesAsync(ct);
        }
    }
}
