using Kairn.Application.Features.Reports;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class TrialBalanceService(AppDbContext db) : ITrialBalanceService
{
    public async Task<TrialBalanceReport> GenerateAsync(Guid tenantId, DateOnly asOf, CancellationToken ct = default)
    {
        // Single GROUP BY query — handles 100K+ lines well under 3 s
        var lineSums = await db.JournalLines
            .Join(
                db.JournalEntries.Where(e => e.TenantId == tenantId && !e.IsDeleted && e.Date <= asOf),
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

        var accounts = await db.Accounts
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

        var rows = accounts.Select(a =>
        {
            var sums = sumByAccount.GetValueOrDefault(a.Id);
            var totalDebit  = sums?.TotalDebit  ?? 0m;
            var totalCredit = sums?.TotalCredit ?? 0m;
            var net = totalDebit - totalCredit;

            // Positive net = debit balance; negative net = credit balance
            var debitBalance  = net > 0 ? net : 0m;
            var creditBalance = net < 0 ? -net : 0m;

            return new TrialBalanceRow(a.Code, a.Name, a.Type, debitBalance, creditBalance);
        }).ToList();

        return new TrialBalanceReport(asOf, rows);
    }
}
