using Kairn.Application.Features.Dashboard;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<DashboardKpis> GetKpisAsync(Guid tenantId, CancellationToken ct = default)
    {
        var today     = DateOnly.FromDateTime(DateTime.UtcNow);
        var currFrom  = new DateOnly(today.Year, today.Month, 1);
        var currTo    = today;
        var priorTo   = currFrom.AddDays(-1);
        var priorFrom = new DateOnly(priorTo.Year, priorTo.Month, 1);

        var (currRev,  currExp)  = await GetMonthlyPnlAsync(tenantId, currFrom,  currTo,  ct);
        var (priorRev, priorExp) = await GetMonthlyPnlAsync(tenantId, priorFrom, priorTo, ct);
        var currAr    = await GetCurrentArAsync(tenantId, ct);
        var priorAr   = await GetArAsOfAsync(tenantId, priorTo, ct);
        var currAp    = await GetCurrentApAsync(tenantId, ct);
        var priorAp   = await GetApAsOfAsync(tenantId, priorTo, ct);
        var currCash  = await GetCashBalanceAsync(tenantId, currTo,  ct);
        var priorCash = await GetCashBalanceAsync(tenantId, priorTo, ct);

        return new DashboardKpis(
            MonthlyRevenue  : new KpiSnapshot(currRev,           priorRev),
            MonthlyExpenses : new KpiSnapshot(currExp,           priorExp),
            NetProfit       : new KpiSnapshot(currRev - currExp, priorRev - priorExp),
            OutstandingAr   : new KpiSnapshot(currAr,            priorAr),
            OutstandingAp   : new KpiSnapshot(currAp,            priorAp),
            CashBalance     : new KpiSnapshot(currCash,          priorCash)
        );
    }

    private async Task<(decimal Revenue, decimal Expenses)> GetMonthlyPnlAsync(
        Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var lineSums = await db.JournalLines
            .Join(
                db.JournalEntries.Where(e =>
                    e.TenantId == tenantId &&
                    !e.IsDeleted &&
                    e.Date >= from &&
                    e.Date <= to),
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

        if (lineSums.Count == 0) return (0m, 0m);

        var accountIds = lineSums.Select(x => x.AccountId).ToList();
        var accounts = await db.Accounts
            .Where(a => accountIds.Contains(a.Id) &&
                        (a.Type == AccountType.Revenue || a.Type == AccountType.Expense))
            .Select(a => new { a.Id, a.Type })
            .ToListAsync(ct);

        var typeById = accounts.ToDictionary(a => a.Id, a => a.Type);
        var sumById  = lineSums.ToDictionary(x => x.AccountId);

        decimal revenue = 0m, expenses = 0m;
        foreach (var (id, type) in typeById)
        {
            var s = sumById.GetValueOrDefault(id);
            if (s is null) continue;
            if (type == AccountType.Revenue)
                revenue  += s.TotalCredit - s.TotalDebit;
            else
                expenses += s.TotalDebit  - s.TotalCredit;
        }
        return (revenue, expenses);
    }

    private async Task<decimal> GetCurrentArAsync(Guid tenantId, CancellationToken ct)
    {
        var invoices = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes).ThenInclude(cn => cn.Lines)
            .Where(i => i.TenantId == tenantId
                     && !i.IsCreditNote
                     && i.Status != InvoiceStatus.Void
                     && i.Status != InvoiceStatus.Draft
                     && i.Status != InvoiceStatus.Paid)
            .ToListAsync(ct);

        return invoices.Sum(i => Math.Max(0m,
            i.Lines.Sum(l => l.LineTotal)
            - i.Payments.Sum(p => p.Amount)
            - i.CreditNotes.Sum(cn => cn.Lines.Sum(l => l.LineTotal))));
    }

    private async Task<decimal> GetArAsOfAsync(Guid tenantId, DateOnly asOf, CancellationToken ct)
    {
        var invoices = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.CreditNotes).ThenInclude(cn => cn.Lines)
            .Where(i => i.TenantId == tenantId
                     && !i.IsCreditNote
                     && i.Status != InvoiceStatus.Void
                     && i.Status != InvoiceStatus.Draft
                     && i.Date <= asOf)
            .ToListAsync(ct);

        return invoices.Sum(i =>
        {
            var balance = i.Lines.Sum(l => l.LineTotal)
                - i.Payments.Where(p => p.Date <= asOf).Sum(p => p.Amount)
                - i.CreditNotes
                    .Where(cn => cn.Date <= asOf)
                    .Sum(cn => cn.Lines.Sum(l => l.LineTotal));
            return Math.Max(0m, balance);
        });
    }

    private async Task<decimal> GetCurrentApAsync(Guid tenantId, CancellationToken ct)
    {
        return await db.Bills
            .Where(b => b.TenantId == tenantId
                     && (b.Status == BillStatus.Approved
                      || b.Status == BillStatus.PartiallyPaid
                      || b.Status == BillStatus.Overdue))
            .SumAsync(b => b.GrandTotal - b.AmountPaid, ct);
    }

    private async Task<decimal> GetApAsOfAsync(Guid tenantId, DateOnly asOf, CancellationToken ct)
    {
        var bills = await db.Bills
            .Include(b => b.Payments)
            .Where(b => b.TenantId == tenantId
                     && b.Status != BillStatus.Void
                     && b.Status != BillStatus.Draft
                     && b.Status != BillStatus.Rejected
                     && b.Date <= asOf)
            .ToListAsync(ct);

        return bills.Sum(b =>
        {
            var paid = b.Payments.Where(p => p.Date <= asOf).Sum(p => p.Amount);
            return Math.Max(0m, b.GrandTotal - paid);
        });
    }

    private async Task<decimal> GetCashBalanceAsync(Guid tenantId, DateOnly asOf, CancellationToken ct)
    {
        var cashAccountIds = await db.Accounts
            .Where(a => a.TenantId == tenantId
                     && a.IsActive
                     && a.Type == AccountType.Asset
                     && a.Code.StartsWith("5"))
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (cashAccountIds.Count == 0) return 0m;

        return await db.JournalLines
            .Join(
                db.JournalEntries.Where(e =>
                    e.TenantId == tenantId &&
                    !e.IsDeleted &&
                    e.Date <= asOf),
                l => l.EntryId,
                e => e.Id,
                (l, _) => l)
            .Where(l => cashAccountIds.Contains(l.AccountId))
            .SumAsync(l => (l.Debit - l.Credit) * l.ExchangeRate, ct);
    }

    // ── Chart data ───────────────────────────────────────────────────────────

    public async Task<DashboardChartData> GetChartDataAsync(Guid tenantId, CancellationToken ct = default)
    {
        var today       = DateOnly.FromDateTime(DateTime.UtcNow);
        var periodStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);

        // ── Revenue & Expenses per month ─────────────────────────────────────
        var rawPnl = await db.JournalLines
            .Join(
                db.JournalEntries.Where(e =>
                    e.TenantId == tenantId && !e.IsDeleted &&
                    e.Date >= periodStart && e.Date <= today),
                l => l.EntryId,
                e => e.Id,
                (l, e) => new
                {
                    l.AccountId,
                    e.Date,
                    Debit  = l.Debit  * l.ExchangeRate,
                    Credit = l.Credit * l.ExchangeRate,
                })
            .ToListAsync(ct);

        var pnlAccountIds = rawPnl.Select(x => x.AccountId).Distinct().ToList();
        var typeById = new Dictionary<Guid, AccountType>();
        if (pnlAccountIds.Count > 0)
        {
            var pnlAccounts = await db.Accounts
                .Where(a => pnlAccountIds.Contains(a.Id) &&
                            (a.Type == AccountType.Revenue || a.Type == AccountType.Expense))
                .Select(a => new { a.Id, a.Type })
                .ToListAsync(ct);
            typeById = pnlAccounts.ToDictionary(a => a.Id, a => a.Type);
        }

        var pnlByMonth = new Dictionary<(int, int), (decimal Rev, decimal Exp)>();
        for (int i = 0; i < 12; i++)
        {
            var d = periodStart.AddMonths(i);
            pnlByMonth[(d.Year, d.Month)] = (0m, 0m);
        }

        foreach (var row in rawPnl)
        {
            if (!typeById.TryGetValue(row.AccountId, out var acctType)) continue;
            var key = (row.Date.Year, row.Date.Month);
            if (!pnlByMonth.TryGetValue(key, out var bucket)) continue;
            if (acctType == AccountType.Revenue)
                pnlByMonth[key] = (bucket.Rev + row.Credit - row.Debit, bucket.Exp);
            else
                pnlByMonth[key] = (bucket.Rev, bucket.Exp + row.Debit - row.Credit);
        }

        var monthlyPnl = Enumerable.Range(0, 12)
            .Select(i =>
            {
                var d = periodStart.AddMonths(i);
                var (rev, exp) = pnlByMonth[(d.Year, d.Month)];
                return new MonthlyPnlPoint(d.Year, d.Month, rev, exp);
            })
            .ToList();

        // ── Cash position (end of each month, running balance) ───────────────
        var cashAccountIds = await db.Accounts
            .Where(a => a.TenantId == tenantId && a.IsActive &&
                        a.Type == AccountType.Asset && a.Code.StartsWith("5"))
            .Select(a => a.Id)
            .ToListAsync(ct);

        List<MonthlyCashPoint> cashPoints;
        if (cashAccountIds.Count == 0)
        {
            cashPoints = Enumerable.Range(0, 12)
                .Select(i => { var d = periodStart.AddMonths(i); return new MonthlyCashPoint(d.Year, d.Month, 0m); })
                .ToList();
        }
        else
        {
            var openingBalance = await db.JournalLines
                .Where(l => cashAccountIds.Contains(l.AccountId))
                .Join(
                    db.JournalEntries.Where(e =>
                        e.TenantId == tenantId && !e.IsDeleted && e.Date < periodStart),
                    l => l.EntryId, e => e.Id, (l, _) => l)
                .SumAsync(l => (l.Debit - l.Credit) * l.ExchangeRate, ct);

            var cashFlows = await db.JournalLines
                .Where(l => cashAccountIds.Contains(l.AccountId))
                .Join(
                    db.JournalEntries.Where(e =>
                        e.TenantId == tenantId && !e.IsDeleted &&
                        e.Date >= periodStart && e.Date <= today),
                    l => l.EntryId, e => e.Id,
                    (l, e) => new { e.Date, Net = (l.Debit - l.Credit) * l.ExchangeRate })
                .ToListAsync(ct);

            var flowByMonth = cashFlows
                .GroupBy(x => (x.Date.Year, x.Date.Month))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Net));

            cashPoints = new List<MonthlyCashPoint>(12);
            decimal running = openingBalance;
            for (int i = 0; i < 12; i++)
            {
                var d = periodStart.AddMonths(i);
                running += flowByMonth.GetValueOrDefault((d.Year, d.Month), 0m);
                cashPoints.Add(new MonthlyCashPoint(d.Year, d.Month, running));
            }
        }

        return new DashboardChartData(monthlyPnl, cashPoints);
    }
}
