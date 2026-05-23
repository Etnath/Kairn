using Kairn.Application.Features.CompanyProfile;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class VatThresholdService(AppDbContext db, ITenantProfileService profileService) : IVatThresholdService
{
    private const decimal DefaultThresholdServices   = 77_700m;
    private const decimal DefaultThresholdCommercial = 188_700m;

    public async Task<VatThresholdStatusDto> GetStatusAsync(Guid tenantId, CancellationToken ct = default)
    {
        var profile = await profileService.GetAsync(tenantId, ct);
        if (profile.BusinessStatus != BusinessStatus.AutoEntrepreneur)
            return new VatThresholdStatusDto(false, 0m, 0m, 0m);

        var threshold  = GetThreshold(profile);
        var ytdRevenue = await ComputeYtdRevenueAsync(tenantId, ct);
        var pct        = threshold > 0 ? Math.Round(ytdRevenue / threshold * 100m, 1) : 0m;
        return new VatThresholdStatusDto(true, ytdRevenue, threshold, pct);
    }

    public async Task RunCheckAsync(Guid tenantId, CancellationToken ct = default)
    {
        var status = await GetStatusAsync(tenantId, ct);
        if (!status.IsAutoEntrepreneur) return;

        var year = DateTime.Today.Year;

        var existingLevels = (await db.VatThresholdAlerts
            .Where(a => a.TenantId == tenantId && a.Year == year)
            .Select(a => a.Level)
            .ToListAsync(ct))
            .ToHashSet();

        var toAdd = new List<VatThresholdAlert>();
        var now   = DateTimeOffset.UtcNow;

        if (status.Pct >= 80m && !existingLevels.Contains(VatThresholdLevel.Warning))
        {
            toAdd.Add(new VatThresholdAlert
            {
                TenantId   = tenantId,
                Year       = year,
                Level      = VatThresholdLevel.Warning,
                YtdRevenue = status.YtdRevenue,
                Threshold  = status.Threshold,
                CreatedAt  = now,
                UpdatedAt  = now,
            });
        }

        if (status.Pct >= 100m && !existingLevels.Contains(VatThresholdLevel.Exceeded))
        {
            toAdd.Add(new VatThresholdAlert
            {
                TenantId   = tenantId,
                Year       = year,
                Level      = VatThresholdLevel.Exceeded,
                YtdRevenue = status.YtdRevenue,
                Threshold  = status.Threshold,
                CreatedAt  = now,
                UpdatedAt  = now,
            });
        }

        if (toAdd.Count > 0)
        {
            db.VatThresholdAlerts.AddRange(toAdd);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<VatThresholdAlertDto>> GetActiveAsync(Guid tenantId, CancellationToken ct = default)
    {
        var year = DateTime.Today.Year;
        return await db.VatThresholdAlerts
            .Where(a => a.TenantId == tenantId && a.Year == year && !a.IsDismissed)
            .OrderBy(a => a.Level)
            .Select(a => new VatThresholdAlertDto(a.Id, a.Year, a.Level, a.YtdRevenue, a.Threshold))
            .ToListAsync(ct);
    }

    public async Task DismissAsync(Guid alertId, string userId, CancellationToken ct = default)
    {
        var alert = await db.VatThresholdAlerts.FindAsync([alertId], ct);
        if (alert is null) return;
        alert.IsDismissed       = true;
        alert.DismissedAt       = DateTimeOffset.UtcNow;
        alert.DismissedByUserId = userId;
        alert.UpdatedAt         = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<decimal> ComputeYtdRevenueAsync(Guid tenantId, CancellationToken ct)
    {
        var yearStart = new DateOnly(DateTime.Today.Year, 1, 1);
        var yearEnd   = new DateOnly(DateTime.Today.Year, 12, 31);

        var invoices = await db.Invoices
            .Include(i => i.Lines)
            .Where(i => i.TenantId == tenantId
                     && i.Date >= yearStart
                     && i.Date <= yearEnd
                     && i.Status != InvoiceStatus.Draft
                     && i.Status != InvoiceStatus.Void)
            .ToListAsync(ct);

        return invoices.Sum(inv =>
        {
            var net = inv.Lines.Sum(l => l.NetAmount);
            return inv.IsCreditNote ? -net : net;
        });
    }

    private static decimal GetThreshold(TenantProfileDto profile) =>
        profile.ActivityType == ActivityType.Commercial
            ? (profile.VatThresholdCommercial > 0 ? profile.VatThresholdCommercial : DefaultThresholdCommercial)
            : (profile.VatThresholdServices   > 0 ? profile.VatThresholdServices   : DefaultThresholdServices);
}
