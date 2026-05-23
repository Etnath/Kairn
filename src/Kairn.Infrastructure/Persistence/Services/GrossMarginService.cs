using Kairn.Application.Features.MarginAnalysis;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class GrossMarginService(AppDbContext db) : IGrossMarginService
{
    public async Task<GrossMarginReport> GenerateAsync(GrossMarginQuery query, CancellationToken ct = default)
    {
        var productLines = await db.ProductLines
            .Include(p => p.Accounts)
            .Where(p => p.TenantId == query.TenantId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        if (productLines.Count == 0)
            return new GrossMarginReport(query.From, query.To, []);

        var allAccountIds = productLines
            .SelectMany(p => p.Accounts.Select(a => a.AccountId))
            .Distinct()
            .ToList();

        var lineSums = await db.JournalLines
            .Where(l => allAccountIds.Contains(l.AccountId))
            .Join(
                db.JournalEntries.Where(e =>
                    e.TenantId == query.TenantId &&
                    !e.IsDeleted &&
                    e.Date >= query.From &&
                    e.Date <= query.To),
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

        var rows = productLines.Select(pl =>
        {
            var revenue = pl.Accounts
                .Where(a => a.Role == ProductLineAccountRole.Revenue)
                .Sum(a =>
                {
                    var s = sumByAccount.GetValueOrDefault(a.AccountId);
                    return (s?.TotalCredit ?? 0m) - (s?.TotalDebit ?? 0m);
                });

            var cogs = pl.Accounts
                .Where(a => a.Role == ProductLineAccountRole.Cogs)
                .Sum(a =>
                {
                    var s = sumByAccount.GetValueOrDefault(a.AccountId);
                    return (s?.TotalDebit ?? 0m) - (s?.TotalCredit ?? 0m);
                });

            return new GrossMarginRow(pl.Id, pl.Name, revenue, cogs, pl.MarginAlertThreshold);
        }).ToList();

        return new GrossMarginReport(query.From, query.To, rows);
    }
}
