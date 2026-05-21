using Kairn.Application.Common;

namespace Kairn.Application.Features.Budgets;

public record BudgetSummaryDto(Guid Id, int FiscalYear, string Name, bool IsActive);

/// <summary>One account row in the budget editor: AccountId, Code, Name, and 12 monthly amounts (index 0 = January).</summary>
public record BudgetAccountRow(Guid AccountId, string Code, string Name, decimal[] Amounts);

public record BudgetDetailDto(
    Guid Id,
    int FiscalYear,
    string Name,
    bool IsActive,
    IReadOnlyList<BudgetAccountRow> RevenueRows,
    IReadOnlyList<BudgetAccountRow> ExpenseRows);

public record CreateBudgetCommand(Guid TenantId, int FiscalYear, string Name);

public record BudgetLineValue(Guid AccountId, int Month, decimal Amount);

public record SaveBudgetLinesCommand(Guid TenantId, Guid BudgetId, IReadOnlyList<BudgetLineValue> Lines);

public record CopyBudgetCommand(
    Guid TenantId,
    Guid SourceBudgetId,
    int TargetFiscalYear,
    string Name,
    decimal AdjustmentPct);

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetSummaryDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<BudgetDetailDto?> GetDetailAsync(Guid tenantId, Guid budgetId, CancellationToken ct = default);
    Task<Result<BudgetSummaryDto>> CreateAsync(CreateBudgetCommand cmd, CancellationToken ct = default);
    Task<Result> SaveLinesAsync(SaveBudgetLinesCommand cmd, CancellationToken ct = default);
    Task<Result<BudgetSummaryDto>> CopyAsync(CopyBudgetCommand cmd, CancellationToken ct = default);
}
