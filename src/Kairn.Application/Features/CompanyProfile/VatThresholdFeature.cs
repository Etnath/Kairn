using Kairn.Domain.Entities;

namespace Kairn.Application.Features.CompanyProfile;

public record VatThresholdStatusDto(
    bool    IsAutoEntrepreneur,
    decimal YtdRevenue,
    decimal Threshold,
    decimal Pct);

public record VatThresholdAlertDto(
    Guid             Id,
    int              Year,
    VatThresholdLevel Level,
    decimal          YtdRevenue,
    decimal          Threshold);

public interface IVatThresholdService
{
    Task<VatThresholdStatusDto>          GetStatusAsync(Guid tenantId, CancellationToken ct = default);
    Task                                 RunCheckAsync (Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<VatThresholdAlertDto>> GetActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task                                 DismissAsync  (Guid alertId, string userId, CancellationToken ct = default);
}
