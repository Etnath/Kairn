namespace Kairn.Application.Features.MarginAnalysis;

public record MarginAlertDto(
    Guid     Id,
    Guid     ProductLineId,
    string   ProductLineName,
    DateOnly Month,
    decimal  MarginPct,
    decimal  ThresholdPct);

public interface IMarginAlertService
{
    Task RunCheckAsync(Guid tenantId, DateOnly month, CancellationToken ct = default);
    Task<IReadOnlyList<MarginAlertDto>> GetActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task DismissAsync(Guid alertId, string userId, CancellationToken ct = default);
}
