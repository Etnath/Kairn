using Kairn.Application.Features.Reports;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class BsService(AppDbContext db) : IBsService
{
    public async Task<BsReport> GenerateAsync(BsQuery query, CancellationToken ct = default)
    {
        var yearStart = new DateOnly(query.AsOf.Year, 1, 1);

        // Cumulative BS account balances up to AsOf
        var bsLineSums = await db.JournalLines
            .Join(db.JournalEntries.Where(e =>
                    e.TenantId == query.TenantId &&
                    !e.IsDeleted &&
                    e.Date <= query.AsOf),
                l => l.EntryId, e => e.Id,
                (l, _) => l)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId   = g.Key,
                TotalDebit  = g.Sum(l => l.Debit  * l.ExchangeRate),
                TotalCredit = g.Sum(l => l.Credit * l.ExchangeRate),
            })
            .ToListAsync(ct);

        // P&L account line sums before current fiscal year (retained earnings)
        var priorLineSums = await db.JournalLines
            .Join(db.JournalEntries.Where(e =>
                    e.TenantId == query.TenantId &&
                    !e.IsDeleted &&
                    e.Date < yearStart),
                l => l.EntryId, e => e.Id,
                (l, _) => l)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId   = g.Key,
                TotalCredit = g.Sum(l => l.Credit * l.ExchangeRate),
                TotalDebit  = g.Sum(l => l.Debit  * l.ExchangeRate),
            })
            .ToListAsync(ct);

        // P&L account line sums for current fiscal year (current year earnings)
        var curYearLineSums = await db.JournalLines
            .Join(db.JournalEntries.Where(e =>
                    e.TenantId == query.TenantId &&
                    !e.IsDeleted &&
                    e.Date >= yearStart &&
                    e.Date <= query.AsOf),
                l => l.EntryId, e => e.Id,
                (l, _) => l)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId   = g.Key,
                TotalCredit = g.Sum(l => l.Credit * l.ExchangeRate),
                TotalDebit  = g.Sum(l => l.Debit  * l.ExchangeRate),
            })
            .ToListAsync(ct);

        var allAccounts = await db.Accounts
            .Where(a => a.TenantId == query.TenantId && a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

        var bsByAcct    = bsLineSums.ToDictionary(x => x.AccountId);
        var priorByAcct = priorLineSums.ToDictionary(x => x.AccountId);
        var curByAcct   = curYearLineSums.ToDictionary(x => x.AccountId);

        var assetLines     = new List<BsAccountLine>();
        var liabilityLines = new List<BsAccountLine>();
        var equityLines    = new List<BsAccountLine>();

        foreach (var acct in allAccounts.Where(a =>
                     a.Type == AccountType.Asset ||
                     a.Type == AccountType.Liability ||
                     a.Type == AccountType.Equity))
        {
            var sums    = bsByAcct.GetValueOrDefault(acct.Id);
            var balance = acct.Type == AccountType.Asset
                ? (sums?.TotalDebit  ?? 0m) - (sums?.TotalCredit ?? 0m)  // assets: debit normal
                : (sums?.TotalCredit ?? 0m) - (sums?.TotalDebit  ?? 0m); // liabilities/equity: credit normal

            if (balance == 0m && query.HideZero) continue;

            var line = new BsAccountLine(acct.Id, acct.Code, acct.Name, acct.IsCurrent, balance);
            switch (acct.Type)
            {
                case AccountType.Asset:     assetLines.Add(line);     break;
                case AccountType.Liability: liabilityLines.Add(line); break;
                case AccountType.Equity:    equityLines.Add(line);    break;
            }
        }

        // Net income = sum(credit − debit) for all P&L accounts.
        // Revenue is credit-normal  → credit−debit > 0 adds to net income.
        // Expense is debit-normal   → credit−debit < 0 reduces net income.
        var retainedEarnings = allAccounts
            .Where(a => a.Type == AccountType.Revenue || a.Type == AccountType.Expense)
            .Sum(a =>
            {
                var s = priorByAcct.GetValueOrDefault(a.Id);
                return (s?.TotalCredit ?? 0m) - (s?.TotalDebit ?? 0m);
            });

        var currentYearEarnings = allAccounts
            .Where(a => a.Type == AccountType.Revenue || a.Type == AccountType.Expense)
            .Sum(a =>
            {
                var s = curByAcct.GetValueOrDefault(a.Id);
                return (s?.TotalCredit ?? 0m) - (s?.TotalDebit ?? 0m);
            });

        var result = new BsReport(query.AsOf, assetLines, liabilityLines, equityLines,
            retainedEarnings, currentYearEarnings);

        if (query.CompareAsOf.HasValue)
        {
            var compReport = await GenerateAsync(
                new BsQuery(query.TenantId, query.CompareAsOf.Value, query.HideZero), ct);
            return result with { Comparison = compReport };
        }

        return result;
    }
}
