using Kairn.Application.Features.Dashboard;
using Kairn.Domain.Entities;

namespace Kairn.Infrastructure.Persistence.Services;

public class DashboardSettingsService(AppDbContext db) : IDashboardSettingsService
{
    public async Task<DashboardSettingsDto> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var s = await db.TenantDashboardSettings.FindAsync([tenantId], ct);
        return s is null
            ? new DashboardSettingsDto(5_000m)
            : new DashboardSettingsDto(s.CashAlertThreshold);
    }

    public async Task SaveAsync(SaveDashboardSettingsCommand command, CancellationToken ct = default)
    {
        var s = await db.TenantDashboardSettings.FindAsync([command.TenantId], ct);
        if (s is null)
        {
            db.TenantDashboardSettings.Add(new TenantDashboardSettings
            {
                TenantId           = command.TenantId,
                CashAlertThreshold = command.CashAlertThreshold,
            });
        }
        else
        {
            s.CashAlertThreshold = command.CashAlertThreshold;
        }
        await db.SaveChangesAsync(ct);
    }
}
