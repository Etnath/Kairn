using Kairn.Application.Features.AP;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class ApAgingService(AppDbContext db) : IApAgingService
{
    public async Task<ApAgingReportDto> GenerateAsync(ApAgingQuery query, CancellationToken ct = default)
    {
        var statuses = query.IncludeZeroBalance
            ? new[] { BillStatus.Approved, BillStatus.PartiallyPaid, BillStatus.Overdue, BillStatus.Paid }
            : new[] { BillStatus.Approved, BillStatus.PartiallyPaid, BillStatus.Overdue };

        var bills = await db.Bills
            .Include(b => b.Vendor)
            .Where(b => b.TenantId == query.TenantId && statuses.Contains(b.Status))
            .ToListAsync(ct);

        var rows = bills
            .GroupBy(b => new { b.VendorId, b.Vendor.Name })
            .Select(g =>
            {
                var agingBills = g.Select(bill =>
                {
                    var outstanding = bill.GrandTotal - bill.AmountPaid;
                    var daysOverdue = query.AsOf.DayNumber - bill.DueDate.DayNumber;
                    var bucket = ClassifyBucket(daysOverdue);
                    return new ApAgingBillDto(
                        bill.Id, bill.Reference, bill.Date, bill.DueDate,
                        Math.Max(0, daysOverdue), outstanding, bucket);
                })
                .Where(b => query.IncludeZeroBalance || b.Outstanding > 0)
                .OrderBy(b => b.DueDate)
                .ToList();

                return new ApAgingRowDto(
                    g.Key.VendorId,
                    g.Key.Name,
                    agingBills.Where(b => b.Bucket == "current").Sum(b => b.Outstanding),
                    agingBills.Where(b => b.Bucket == "1-30").Sum(b => b.Outstanding),
                    agingBills.Where(b => b.Bucket == "31-60").Sum(b => b.Outstanding),
                    agingBills.Where(b => b.Bucket == "61-90").Sum(b => b.Outstanding),
                    agingBills.Where(b => b.Bucket == "91+").Sum(b => b.Outstanding),
                    agingBills.Sum(b => b.Outstanding),
                    agingBills);
            })
            .Where(r => query.IncludeZeroBalance || r.Total > 0)
            .OrderByDescending(r => r.Total)
            .ToList();

        if (!string.IsNullOrWhiteSpace(query.VendorFilter))
        {
            var filter = query.VendorFilter.Trim();
            rows = rows
                .Where(r => r.VendorName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new ApAgingReportDto(query.AsOf, rows);
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
