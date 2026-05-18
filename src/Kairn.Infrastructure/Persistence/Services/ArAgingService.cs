using Kairn.Application.Features.AR;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class ArAgingService(AppDbContext db) : IArAgingService
{
    public async Task<ArAgingReportDto> GenerateAsync(ArAgingQuery query, CancellationToken ct = default)
    {
        var statuses = query.IncludeZeroBalance
            ? new[] { InvoiceStatus.Sent, InvoiceStatus.PartiallyPaid, InvoiceStatus.Overdue, InvoiceStatus.Paid }
            : new[] { InvoiceStatus.Sent, InvoiceStatus.PartiallyPaid, InvoiceStatus.Overdue };

        var invoices = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes).ThenInclude(cn => cn.Lines)
            .Where(i => i.TenantId == query.TenantId && !i.IsCreditNote && statuses.Contains(i.Status))
            .ToListAsync(ct);

        var rows = invoices
            .GroupBy(i => new { i.CustomerId, i.Customer.Name })
            .Select(g =>
            {
                var agingInvoices = g.Select(inv =>
                {
                    var grandTotal = inv.Lines.Sum(l => l.LineTotal);
                    var paid = inv.Payments.Sum(p => p.Amount)
                              + inv.CreditNotes.Sum(cn => cn.Lines.Sum(l => l.LineTotal));
                    var outstanding = grandTotal - paid;
                    var daysOverdue = query.AsOf.DayNumber - inv.DueDate.DayNumber;
                    var bucket = ClassifyBucket(daysOverdue);
                    return new ArAgingInvoiceDto(
                        inv.Id, inv.Reference, inv.Date, inv.DueDate,
                        Math.Max(0, daysOverdue), outstanding, bucket);
                })
                .Where(i => query.IncludeZeroBalance || i.Outstanding > 0)
                .OrderBy(i => i.DueDate)
                .ToList();

                return new ArAgingRowDto(
                    g.Key.CustomerId,
                    g.Key.Name,
                    agingInvoices.Where(i => i.Bucket == "current").Sum(i => i.Outstanding),
                    agingInvoices.Where(i => i.Bucket == "1-30").Sum(i => i.Outstanding),
                    agingInvoices.Where(i => i.Bucket == "31-60").Sum(i => i.Outstanding),
                    agingInvoices.Where(i => i.Bucket == "61-90").Sum(i => i.Outstanding),
                    agingInvoices.Where(i => i.Bucket == "91+").Sum(i => i.Outstanding),
                    agingInvoices.Sum(i => i.Outstanding),
                    agingInvoices);
            })
            .Where(r => query.IncludeZeroBalance || r.Total > 0)
            .OrderByDescending(r => r.Total)
            .ToList();

        if (!string.IsNullOrWhiteSpace(query.CustomerFilter))
        {
            var filter = query.CustomerFilter.Trim();
            rows = rows
                .Where(r => r.CustomerName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new ArAgingReportDto(query.AsOf, rows);
    }

    private static string ClassifyBucket(int daysOverdue) => daysOverdue switch
    {
        <= 0  => "current",
        <= 30 => "1-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        _     => "91+"
    };
}
