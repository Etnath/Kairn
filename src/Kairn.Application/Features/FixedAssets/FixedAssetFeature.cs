using Kairn.Application.Common;
using Kairn.Domain.Entities;

namespace Kairn.Application.Features.FixedAssets;

public record FixedAssetDto(
    Guid Id,
    string Name,
    string? Category,
    DateOnly PurchaseDate,
    decimal PurchaseValue,
    decimal ResidualValue,
    DepreciationMethod Method,
    int UsefulLifeYears,
    Guid AssetAccountId,
    string AssetAccountCode,
    string AssetAccountName,
    Guid AccumulatedDepreciationAccountId,
    string AccumDeprecAccountCode,
    string AccumDeprecAccountName,
    Guid? DepreciationExpenseAccountId,
    string? DeprecExpenseAccountCode,
    string? DeprecExpenseAccountName,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    DateOnly? LastDepreciatedDate,
    DateOnly? NextDepreciationDate,
    bool HasDepreciationPostings,
    bool IsFullyDepreciated,
    bool IsActive,
    DateOnly? DisposalDate);

public record CreateFixedAssetCommand(
    Guid TenantId,
    string Name,
    string? Category,
    DateOnly PurchaseDate,
    decimal PurchaseValue,
    decimal ResidualValue,
    DepreciationMethod Method,
    int UsefulLifeYears,
    Guid AssetAccountId,
    Guid AccumulatedDepreciationAccountId,
    Guid? DepreciationExpenseAccountId,
    string CreatedByUserId,
    string CreatedByName);

public record UpdateFixedAssetCommand(
    Guid Id,
    Guid TenantId,
    string? Category,
    decimal ResidualValue,
    DepreciationMethod Method,
    int UsefulLifeYears,
    Guid AssetAccountId,
    Guid AccumulatedDepreciationAccountId,
    Guid? DepreciationExpenseAccountId);

public record DisposeFixedAssetCommand(
    Guid Id,
    Guid TenantId,
    DateOnly DisposalDate,
    Guid? GainLossAccountId,
    string? Notes,
    string CreatedByUserId,
    string CreatedByName);

public record DepreciationAssetFailure(Guid AssetId, string AssetName, DateOnly Period, string Error);

public record DepreciationRunResult(
    int Posted,
    int Skipped,
    int Failed,
    IReadOnlyList<DepreciationAssetFailure> Failures);

public interface IFixedAssetService
{
    Task<IReadOnlyList<FixedAssetDto>> GetAllAsync(Guid tenantId, bool includeInactive = false, CancellationToken ct = default);
    Task<Result<FixedAssetDto>> CreateAsync(CreateFixedAssetCommand cmd, CancellationToken ct = default);
    Task<Result<FixedAssetDto>> UpdateAsync(UpdateFixedAssetCommand cmd, CancellationToken ct = default);
    Task<Result<FixedAssetDto>> DisposeAsync(DisposeFixedAssetCommand cmd, CancellationToken ct = default);
    Task<DepreciationRunResult> RunDepreciationAsync(Guid tenantId, DateOnly? upToMonth, string createdByUserId, string createdByName, CancellationToken ct = default);
    Task<int> GetRecentFailureCountAsync(Guid tenantId, CancellationToken ct = default);
}
