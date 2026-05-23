using Kairn.Application.Features.MarginAnalysis;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class MarginTrendService(AppDbContext db) : IMarginTrendService
{
    public async Task<MarginTrendReport> GenerateAsync(MarginTrendQuery query, CancellationToken ct = default)
    {
        var productLines = await db.ProductLines
            .Include(p => p.Accounts)
            .Where(p => p.TenantId == query.TenantId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        if (productLines.Count == 0)
            return new MarginTrendReport(query.From, query.To, []);

        var allAccountIds = productLines
            .SelectMany(p => p.Accounts.Select(a => a.AccountId))
            .Distinct()
            .ToList();

        // Load all journal lines in the period for linked accounts, tagged with year/month
        var rawLines = await db.JournalLines
            .Where(l => allAccountIds.Contains(l.AccountId))
            .Join(
                db.JournalEntries.Where(e =>
                    e.TenantId == query.TenantId && !e.IsDeleted &&
                    e.Date >= query.From && e.Date <= query.To),
                l => l.EntryId, e => e.Id,
                (l, e) => new
                {
                    l.AccountId,
                    Year   = e.Date.Year,
                    Month  = e.Date.Month,
                    Debit  = l.Debit  * l.ExchangeRate,
                    Credit = l.Credit * l.ExchangeRate,
                })
            .ToListAsync(ct);

        // Group into a dictionary keyed by (AccountId, Year, Month)
        var grouped = rawLines
            .GroupBy(x => (x.AccountId, x.Year, x.Month))
            .ToDictionary(
                g => g.Key,
                g => (TotalDebit: g.Sum(x => x.Debit), TotalCredit: g.Sum(x => x.Credit)));

        var months = BuildMonthList(query.From, query.To);

        var series = productLines.Select(pl =>
        {
            var points = months.Select(m =>
            {
                var revenue = pl.Accounts
                    .Where(a => a.Role == ProductLineAccountRole.Revenue)
                    .Sum(a =>
                    {
                        grouped.TryGetValue((a.AccountId, m.Year, m.Month), out var s);
                        return s.TotalCredit - s.TotalDebit;
                    });

                var cogs = pl.Accounts
                    .Where(a => a.Role == ProductLineAccountRole.Cogs)
                    .Sum(a =>
                    {
                        grouped.TryGetValue((a.AccountId, m.Year, m.Month), out var s);
                        return s.TotalDebit - s.TotalCredit;
                    });

                return new MarginTrendPoint(m, revenue, cogs);
            }).ToList();

            return new MarginTrendSeries(pl.Id, pl.Name, points);
        }).ToList();

        return new MarginTrendReport(query.From, query.To, series);
    }

    private static List<DateOnly> BuildMonthList(DateOnly from, DateOnly to)
    {
        var months = new List<DateOnly>();
        var current = new DateOnly(from.Year, from.Month, 1);
        var end     = new DateOnly(to.Year, to.Month, 1);
        while (current <= end)
        {
            months.Add(current);
            current = current.AddMonths(1);
        }
        return months;
    }
}
