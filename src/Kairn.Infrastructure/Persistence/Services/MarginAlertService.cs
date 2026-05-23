using Kairn.Application.Features.MarginAnalysis;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class MarginAlertService(AppDbContext db) : IMarginAlertService
{
    public async Task RunCheckAsync(Guid tenantId, DateOnly month, CancellationToken ct = default)
    {
        var productLines = await db.ProductLines
            .Include(p => p.Accounts)
            .Where(p => p.TenantId == tenantId && p.IsActive && p.MarginAlertThreshold.HasValue)
            .ToListAsync(ct);

        if (productLines.Count == 0) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var from  = month;
        var to    = today < month.AddMonths(1) ? today : month.AddMonths(1).AddDays(-1);

        var allAccountIds = productLines
            .SelectMany(p => p.Accounts.Select(a => a.AccountId))
            .Distinct()
            .ToList();

        var lineSums = await db.JournalLines
            .Where(l => allAccountIds.Contains(l.AccountId))
            .Join(
                db.JournalEntries.Where(e =>
                    e.TenantId == tenantId && !e.IsDeleted &&
                    e.Date >= from && e.Date <= to),
                l => l.EntryId,
                e => e.Id,
                (l, _) => l)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId   = g.Key,
                TotalDebit  = g.Sum(l => l.Debit  * l.ExchangeRate),
                TotalCredit = g.Sum(l => l.Credit * l.ExchangeRate),
            })
            .ToListAsync(ct);

        var sumByAccount = lineSums.ToDictionary(x => x.AccountId);

        // Load existing alerts for this tenant/month to suppress duplicates
        var existingIds = await db.MarginAlerts
            .Where(a => a.TenantId == tenantId && a.Month == month)
            .Select(a => a.ProductLineId)
            .ToListAsync(ct);

        var existingSet = new HashSet<Guid>(existingIds);

        var toAdd = new List<MarginAlert>();

        foreach (var pl in productLines)
        {
            if (existingSet.Contains(pl.Id)) continue;

            var revenue = pl.Accounts
                .Where(a => a.Role == ProductLineAccountRole.Revenue)
                .Sum(a =>
                {
                    var s = sumByAccount.GetValueOrDefault(a.AccountId);
                    return (s?.TotalCredit ?? 0m) - (s?.TotalDebit ?? 0m);
                });

            if (revenue <= 0m) continue;

            var cogs = pl.Accounts
                .Where(a => a.Role == ProductLineAccountRole.Cogs)
                .Sum(a =>
                {
                    var s = sumByAccount.GetValueOrDefault(a.AccountId);
                    return (s?.TotalDebit ?? 0m) - (s?.TotalCredit ?? 0m);
                });

            var grossProfit = revenue - cogs;
            var marginPct   = Math.Round((grossProfit / revenue) * 100m, 1);
            var threshold   = pl.MarginAlertThreshold!.Value;

            if (marginPct < threshold)
            {
                toAdd.Add(new MarginAlert
                {
                    TenantId        = tenantId,
                    ProductLineId   = pl.Id,
                    ProductLineName = pl.Name,
                    Month           = month,
                    MarginPct       = marginPct,
                    ThresholdPct    = threshold,
                    CreatedAt       = DateTimeOffset.UtcNow,
                    UpdatedAt       = DateTimeOffset.UtcNow,
                });
            }
        }

        if (toAdd.Count > 0)
        {
            db.MarginAlerts.AddRange(toAdd);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<MarginAlertDto>> GetActiveAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await db.MarginAlerts
            .Where(a => a.TenantId == tenantId && !a.IsDismissed)
            .OrderByDescending(a => a.Month)
            .ThenBy(a => a.ProductLineName)
            .Select(a => new MarginAlertDto(
                a.Id, a.ProductLineId, a.ProductLineName,
                a.Month, a.MarginPct, a.ThresholdPct))
            .ToListAsync(ct);
    }

    public async Task DismissAsync(Guid alertId, string userId, CancellationToken ct = default)
    {
        var alert = await db.MarginAlerts.FindAsync([alertId], ct);
        if (alert is null) return;
        alert.IsDismissed       = true;
        alert.DismissedAt       = DateTimeOffset.UtcNow;
        alert.DismissedByUserId = userId;
        alert.UpdatedAt         = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
