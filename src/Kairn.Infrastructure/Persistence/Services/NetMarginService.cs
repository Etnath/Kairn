using Kairn.Application.Features.MarginAnalysis;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class NetMarginService(AppDbContext db) : INetMarginService
{
    public async Task<NetMarginReport> GenerateAsync(GrossMarginQuery query, CancellationToken ct = default)
    {
        var productLines = await db.ProductLines
            .Include(p => p.Accounts)
            .Where(p => p.TenantId == query.TenantId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        if (productLines.Count == 0)
            return new NetMarginReport(query.From, query.To, 0m, false, []);

        bool anyRules = productLines.Any(p => p.OpExAllocationPct is > 0);

        var allProductLineAccountIds = productLines
            .SelectMany(p => p.Accounts.Select(a => a.AccountId))
            .Distinct()
            .ToList();

        // Sums for product line accounts (revenue + COGS)
        var plLineSums = await db.JournalLines
            .Where(l => allProductLineAccountIds.Contains(l.AccountId))
            .Join(
                db.JournalEntries.Where(e =>
                    e.TenantId == query.TenantId && !e.IsDeleted &&
                    e.Date >= query.From && e.Date <= query.To),
                l => l.EntryId, e => e.Id, (l, _) => l)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId   = g.Key,
                TotalDebit  = g.Sum(l => l.Debit  * l.ExchangeRate),
                TotalCredit = g.Sum(l => l.Credit * l.ExchangeRate),
            })
            .ToListAsync(ct);

        var sumByAccount = plLineSums.ToDictionary(x => x.AccountId);

        // Total OpEx = all Expense accounts NOT linked to any product line
        var opExAccountIds = await db.Accounts
            .Where(a => a.TenantId == query.TenantId &&
                        a.Type == AccountType.Expense &&
                        !allProductLineAccountIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync(ct);

        decimal totalOpEx = 0m;
        if (opExAccountIds.Count > 0)
        {
            var opExLines = await db.JournalLines
                .Where(l => opExAccountIds.Contains(l.AccountId))
                .Join(
                    db.JournalEntries.Where(e =>
                        e.TenantId == query.TenantId && !e.IsDeleted &&
                        e.Date >= query.From && e.Date <= query.To),
                    l => l.EntryId, e => e.Id,
                    (l, _) => new { Debit = l.Debit * l.ExchangeRate, Credit = l.Credit * l.ExchangeRate })
                .ToListAsync(ct);

            totalOpEx = opExLines.Sum(x => x.Debit) - opExLines.Sum(x => x.Credit);
        }

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

            var allocatedOpEx = pl.OpExAllocationPct is > 0
                ? Math.Round(totalOpEx * pl.OpExAllocationPct.Value / 100m, 4)
                : 0m;

            return new NetMarginRow(pl.Id, pl.Name, revenue, cogs, allocatedOpEx, pl.MarginAlertThreshold);
        }).ToList();

        return new NetMarginReport(query.From, query.To, totalOpEx, anyRules, rows);
    }
}
