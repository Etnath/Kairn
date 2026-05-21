using Kairn.Application.Features.Reports;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class PnlService(AppDbContext db) : IPnlService
{
    public async Task<PnlReport> GenerateAsync(PnlQuery query, CancellationToken ct = default)
    {
        var lineSums = await db.JournalLines
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

        var accounts = await db.Accounts
            .Where(a => a.TenantId == query.TenantId && a.IsActive &&
                        (a.Type == AccountType.Revenue || a.Type == AccountType.Expense))
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

        var lines = new List<PnlAccountLine>();

        foreach (var acct in accounts)
        {
            var sums = sumByAccount.GetValueOrDefault(acct.Id);

            // Revenue = net credit activity; Expense = net debit activity
            var amount = acct.Type == AccountType.Revenue
                ? (sums?.TotalCredit ?? 0m) - (sums?.TotalDebit ?? 0m)
                : (sums?.TotalDebit  ?? 0m) - (sums?.TotalCredit ?? 0m);

            if (amount == 0m && query.HideZero) continue;

            lines.Add(new PnlAccountLine(acct.Id, acct.Code, acct.Name, ClassifyAccount(acct), amount));
        }

        return new PnlReport(query.From, query.To, lines);
    }

    public async Task<IReadOnlyList<PnlDrillDownLine>> GetDrillDownAsync(
        Guid tenantId, Guid accountId, bool isRevenue,
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var rows = await db.JournalLines
            .Where(l => l.AccountId == accountId)
            .Join(
                db.JournalEntries.Where(e =>
                    e.TenantId == tenantId &&
                    !e.IsDeleted &&
                    e.Date >= from &&
                    e.Date <= to),
                l => l.EntryId,
                e => e.Id,
                (l, e) => new
                {
                    EntryId     = e.Id,
                    e.Date,
                    e.Reference,
                    Description = e.Description,
                    Debit       = l.Debit  * l.ExchangeRate,
                    Credit      = l.Credit * l.ExchangeRate,
                    Memo        = l.Memo,
                })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Reference)
            .ToListAsync(ct);

        return rows
            .Select(x => new PnlDrillDownLine(
                x.EntryId,
                x.Date,
                x.Reference,
                x.Description,
                isRevenue ? x.Credit - x.Debit : x.Debit - x.Credit,
                x.Memo))
            .ToList();
    }

    public async Task<PnlBudgetReport?> GetBudgetOverlayAsync(
        PnlReport actual, Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var years = Enumerable.Range(from.Year, to.Year - from.Year + 1).ToList();

        var rawLines = await db.BudgetLines
            .Join(
                db.Budgets.Where(b => b.TenantId == tenantId && b.IsActive && years.Contains(b.FiscalYear)),
                l => l.BudgetId,
                b => b.Id,
                (l, b) => new { l.AccountId, l.Month, l.Amount, b.FiscalYear })
            .ToListAsync(ct);

        if (rawLines.Count == 0) return null;

        // Filter to months that overlap the date range in C# to avoid EF DateOnly translation issues
        var budgetByAccount = rawLines
            .Where(r =>
            {
                var monthStart = new DateOnly(r.FiscalYear, r.Month, 1);
                var monthEnd   = new DateOnly(r.FiscalYear, r.Month, DateTime.DaysInMonth(r.FiscalYear, r.Month));
                return monthStart <= to && monthEnd >= from;
            })
            .GroupBy(r => r.AccountId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        if (budgetByAccount.Count == 0) return null;

        var budgetOnlyIds = budgetByAccount.Keys
            .Where(id => actual.Lines.All(l => l.AccountId != id))
            .ToList();

        var budgetOnlyAccounts = budgetOnlyIds.Count > 0
            ? await db.Accounts.Where(a => budgetOnlyIds.Contains(a.Id)).ToListAsync(ct)
            : [];

        var lines = new List<PnlBudgetLine>();

        foreach (var l in actual.Lines)
            lines.Add(new PnlBudgetLine(l.AccountId, l.AccountCode, l.AccountName, l.Group,
                l.Amount, budgetByAccount.GetValueOrDefault(l.AccountId)));

        foreach (var acct in budgetOnlyAccounts)
            lines.Add(new PnlBudgetLine(acct.Id, acct.Code, acct.Name, ClassifyAccount(acct),
                0m, budgetByAccount[acct.Id]));

        return new PnlBudgetReport(actual,
            lines.OrderBy(l => l.Group).ThenBy(l => l.AccountCode).ToList());
    }

    public async Task<YearEndForecastReport?> GetYearEndForecastAsync(
        Guid tenantId, int fiscalYear, int actualThroughMonth, CancellationToken ct = default)
    {
        actualThroughMonth = Math.Clamp(actualThroughMonth, 1, 12);

        var budgetExists = await db.Budgets
            .AnyAsync(b => b.TenantId == tenantId && b.FiscalYear == fiscalYear && b.IsActive, ct);
        if (!budgetExists) return null;

        var ytdFrom = new DateOnly(fiscalYear, 1, 1);
        var ytdTo   = new DateOnly(fiscalYear, actualThroughMonth,
            DateTime.DaysInMonth(fiscalYear, actualThroughMonth));

        var actualReport = await GenerateAsync(
            new PnlQuery(tenantId, ytdFrom, ytdTo, HideZero: false), ct);

        var allBudgetLines = await db.BudgetLines
            .Join(
                db.Budgets.Where(b => b.TenantId == tenantId && b.FiscalYear == fiscalYear && b.IsActive),
                l => l.BudgetId,
                b => b.Id,
                (l, b) => new { l.AccountId, l.Month, l.Amount })
            .ToListAsync(ct);

        var remainingByAccount = allBudgetLines
            .Where(l => l.Month > actualThroughMonth)
            .GroupBy(l => l.AccountId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        var annualByAccount = allBudgetLines
            .GroupBy(l => l.AccountId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        var actualDict    = actualReport.Lines.ToDictionary(l => l.AccountId);
        var budgetOnlyIds = annualByAccount.Keys.Where(id => !actualDict.ContainsKey(id)).ToList();

        var budgetOnlyAccounts = budgetOnlyIds.Count > 0
            ? await db.Accounts.Where(a => budgetOnlyIds.Contains(a.Id)).ToListAsync(ct)
            : [];
        var budgetOnlyDict = budgetOnlyAccounts.ToDictionary(a => a.Id);

        var lines = new List<ForecastLine>();

        foreach (var l in actualReport.Lines)
            lines.Add(new ForecastLine(
                l.AccountId, l.AccountCode, l.AccountName, l.Group,
                l.Amount,
                remainingByAccount.GetValueOrDefault(l.AccountId),
                annualByAccount.GetValueOrDefault(l.AccountId)));

        foreach (var id in budgetOnlyIds)
        {
            if (!budgetOnlyDict.TryGetValue(id, out var acct)) continue;
            lines.Add(new ForecastLine(
                acct.Id, acct.Code, acct.Name, ClassifyAccount(acct),
                0m,
                remainingByAccount.GetValueOrDefault(id),
                annualByAccount.GetValueOrDefault(id)));
        }

        var filtered = lines
            .Where(l => l.YtdActual != 0m || l.AnnualBudget != 0m)
            .OrderBy(l => l.Group)
            .ThenBy(l => l.AccountCode)
            .ToList();

        return new YearEndForecastReport(fiscalYear, actualThroughMonth, filtered);
    }

    private static PnlGroup ClassifyAccount(Account acct)
    {
        if (acct.Type == AccountType.Revenue) return PnlGroup.Revenue;

        // Expense accounts: classify by code prefix (French PCG)
        if (acct.Code.Length >= 2)
        {
            var prefix2 = acct.Code[..2];
            if (prefix2 is "60" or "61") return PnlGroup.Cogs;
            if (prefix2 == "66") return PnlGroup.Interest;
            if (prefix2 == "69") return PnlGroup.Tax;
            if (prefix2 == "68") return PnlGroup.Depreciation;
        }
        return PnlGroup.OperatingExpenses;
    }
}
