using Kairn.Application.Common;
using Kairn.Application.Features.AP;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class ApSettingsService(AppDbContext db) : IApSettingsService
{
    public async Task<ApSettingsDto> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var settings = await db.TenantApSettings.FindAsync([tenantId], ct);
        return settings is null
            ? new ApSettingsDto(false, 1000m, "Admin")
            : new ApSettingsDto(settings.ApprovalEnabled, settings.ApprovalThreshold, settings.ApproverRoles);
    }

    public async Task<Result> SaveAsync(SaveApSettingsCommand cmd, CancellationToken ct = default)
    {
        var settings = await db.TenantApSettings.FindAsync([cmd.TenantId], ct);
        if (settings is null)
        {
            db.TenantApSettings.Add(new TenantApSettings
            {
                TenantId = cmd.TenantId,
                ApprovalEnabled = cmd.ApprovalEnabled,
                ApprovalThreshold = cmd.ApprovalThreshold,
                ApproverRoles = cmd.ApproverRoles,
            });
        }
        else
        {
            settings.ApprovalEnabled = cmd.ApprovalEnabled;
            settings.ApprovalThreshold = cmd.ApprovalThreshold;
            settings.ApproverRoles = cmd.ApproverRoles;
        }
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
