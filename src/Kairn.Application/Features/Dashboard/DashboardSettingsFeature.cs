namespace Kairn.Application.Features.Dashboard;

public record DashboardSettingsDto(decimal CashAlertThreshold);

public record SaveDashboardSettingsCommand(Guid TenantId, decimal CashAlertThreshold);

public interface IDashboardSettingsService
{
    Task<DashboardSettingsDto> GetAsync(Guid tenantId, CancellationToken ct = default);
    Task SaveAsync(SaveDashboardSettingsCommand command, CancellationToken ct = default);
}
