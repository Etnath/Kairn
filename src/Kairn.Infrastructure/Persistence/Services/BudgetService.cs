using Kairn.Application.Common;
using Kairn.Application.Features.Budgets;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class BudgetService(AppDbContext db) : IBudgetService
{
    public async Task<IReadOnlyList<BudgetSummaryDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await db.Budgets
            .Where(b => b.TenantId == tenantId)
            .OrderByDescending(b => b.FiscalYear)
            .Select(b => new BudgetSummaryDto(b.Id, b.FiscalYear, b.Name, b.IsActive))
            .ToListAsync(ct);
    }

    public async Task<BudgetDetailDto?> GetDetailAsync(Guid tenantId, Guid budgetId, CancellationToken ct = default)
    {
        var budget = await db.Budgets
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Id == budgetId, ct);

        if (budget is null) return null;

        var accounts = await db.Accounts
            .Where(a => a.TenantId == tenantId && a.IsActive &&
                        (a.Type == AccountType.Revenue || a.Type == AccountType.Expense))
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

        var linesByAccount = budget.Lines
            .GroupBy(l => l.AccountId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(l => l.Month, l => l.Amount));

        var revenueRows = accounts
            .Where(a => a.Type == AccountType.Revenue)
            .Select(a => BuildRow(a, linesByAccount))
            .ToList();

        var expenseRows = accounts
            .Where(a => a.Type == AccountType.Expense)
            .Select(a => BuildRow(a, linesByAccount))
            .ToList();

        return new BudgetDetailDto(budget.Id, budget.FiscalYear, budget.Name, budget.IsActive,
            revenueRows, expenseRows);
    }

    public async Task<Result<BudgetSummaryDto>> CreateAsync(CreateBudgetCommand cmd, CancellationToken ct = default)
    {
        bool duplicate = await db.Budgets.AnyAsync(
            b => b.TenantId == cmd.TenantId && b.FiscalYear == cmd.FiscalYear, ct);

        if (duplicate)
            return Result<BudgetSummaryDto>.Fail($"DuplicateYear:{cmd.FiscalYear}");

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            TenantId = cmd.TenantId,
            FiscalYear = cmd.FiscalYear,
            Name = cmd.Name,
            IsActive = true,
        };

        db.Budgets.Add(budget);
        await db.SaveChangesAsync(ct);

        return Result<BudgetSummaryDto>.Ok(new BudgetSummaryDto(budget.Id, budget.FiscalYear, budget.Name, budget.IsActive));
    }

    public async Task<Result> SaveLinesAsync(SaveBudgetLinesCommand cmd, CancellationToken ct = default)
    {
        var budget = await db.Budgets
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.TenantId == cmd.TenantId && b.Id == cmd.BudgetId, ct);

        if (budget is null)
            return Result.Fail("Budget not found.");

        // Replace all lines
        db.BudgetLines.RemoveRange(budget.Lines);

        var newLines = cmd.Lines
            .Where(l => l.Amount != 0m)
            .Select(l => new BudgetLine
            {
                Id = Guid.NewGuid(),
                BudgetId = budget.Id,
                AccountId = l.AccountId,
                Month = l.Month,
                Amount = l.Amount,
            });

        db.BudgetLines.AddRange(newLines);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result<BudgetSummaryDto>> CopyAsync(CopyBudgetCommand cmd, CancellationToken ct = default)
    {
        var source = await db.Budgets
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.TenantId == cmd.TenantId && b.Id == cmd.SourceBudgetId, ct);

        if (source is null)
            return Result<BudgetSummaryDto>.Fail("Source budget not found.");

        bool duplicate = await db.Budgets.AnyAsync(
            b => b.TenantId == cmd.TenantId && b.FiscalYear == cmd.TargetFiscalYear, ct);

        if (duplicate)
            return Result<BudgetSummaryDto>.Fail($"DuplicateYear:{cmd.TargetFiscalYear}");

        var factor  = 1m + (cmd.AdjustmentPct / 100m);
        var newBudget = new Budget
        {
            Id         = Guid.NewGuid(),
            TenantId   = cmd.TenantId,
            FiscalYear = cmd.TargetFiscalYear,
            Name       = cmd.Name,
            IsActive   = true,
        };

        newBudget.Lines = source.Lines
            .Where(l => l.Amount != 0m)
            .Select(l => new BudgetLine
            {
                Id        = Guid.NewGuid(),
                BudgetId  = newBudget.Id,
                AccountId = l.AccountId,
                Month     = l.Month,
                Amount    = Math.Round(l.Amount * factor, 2),
            })
            .ToList();

        db.Budgets.Add(newBudget);
        await db.SaveChangesAsync(ct);

        return Result<BudgetSummaryDto>.Ok(
            new BudgetSummaryDto(newBudget.Id, newBudget.FiscalYear, newBudget.Name, newBudget.IsActive));
    }

    private static BudgetAccountRow BuildRow(Account acct, Dictionary<Guid, Dictionary<int, decimal>> linesByAccount)
    {
        var amounts = new decimal[12];
        if (linesByAccount.TryGetValue(acct.Id, out var byMonth))
        {
            for (int m = 1; m <= 12; m++)
                amounts[m - 1] = byMonth.GetValueOrDefault(m);
        }
        return new BudgetAccountRow(acct.Id, acct.Code, acct.Name, amounts);
    }
}
