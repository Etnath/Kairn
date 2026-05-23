using Kairn.Application.Common;

namespace Kairn.Application.Features.MarginAnalysis;

public record ProductLineAccountDto(Guid AccountId, string AccountCode, string AccountName);

public record ProductLineDto(
    Guid Id,
    string Name,
    string? Description,
    decimal? MarginAlertThreshold,
    decimal? OpExAllocationPct,
    bool IsActive,
    IReadOnlyList<ProductLineAccountDto> RevenueAccounts,
    IReadOnlyList<ProductLineAccountDto> CogsAccounts);

public record CreateProductLineCommand(
    Guid TenantId,
    string Name,
    string? Description,
    decimal? MarginAlertThreshold,
    decimal? OpExAllocationPct,
    IReadOnlyList<Guid> RevenueAccountIds,
    IReadOnlyList<Guid> CogsAccountIds);

public record UpdateProductLineCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    decimal? MarginAlertThreshold,
    decimal? OpExAllocationPct,
    bool IsActive,
    IReadOnlyList<Guid> RevenueAccountIds,
    IReadOnlyList<Guid> CogsAccountIds);

public interface IProductLineService
{
    Task<List<ProductLineDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<ProductLineDto>> CreateAsync(CreateProductLineCommand cmd, CancellationToken ct = default);
    Task<Result<ProductLineDto>> UpdateAsync(UpdateProductLineCommand cmd, CancellationToken ct = default);
}
