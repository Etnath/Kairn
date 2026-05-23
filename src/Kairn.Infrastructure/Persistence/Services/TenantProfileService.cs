using System.Text.Json;
using Kairn.Application.Common;
using Kairn.Application.Features.CompanyProfile;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class TenantProfileService(AppDbContext db) : ITenantProfileService
{
    private const decimal DefaultThresholdServices  = 77_700m;
    private const decimal DefaultThresholdCommercial = 188_700m;

    public async Task<TenantProfileDto> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var profile = await db.TenantProfiles.FindAsync([tenantId], ct);
        return profile is null ? DefaultDto(tenantId) : ToDto(profile);
    }

    public async Task<Result> SaveAsync(SaveTenantProfileCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.LegalName))
            return Result.Fail("Legal name is required.");

        if (!string.IsNullOrWhiteSpace(cmd.Siret) &&
            (cmd.Siret.Length != 14 || !cmd.Siret.All(char.IsDigit)))
            return Result.Fail("SIRET must be exactly 14 digits.");

        if (cmd.VatThresholdServices <= 0 || cmd.VatThresholdCommercial <= 0)
            return Result.Fail("VAT thresholds must be greater than zero.");

        var profile = await db.TenantProfiles.FindAsync([cmd.TenantId], ct);
        string? oldValues = null;

        if (profile is null)
        {
            profile = new TenantProfile { TenantId = cmd.TenantId };
            db.TenantProfiles.Add(profile);
        }
        else
        {
            oldValues = JsonSerializer.Serialize(new
            {
                profile.LegalName, profile.Siret, profile.BusinessStatus,
                profile.ActivityType, profile.VatThresholdServices, profile.VatThresholdCommercial,
            });
        }

        profile.LegalName              = cmd.LegalName.Trim();
        profile.Siret                  = cmd.Siret.Trim();
        profile.AddressLine            = cmd.AddressLine.Trim();
        profile.PostalCode             = cmd.PostalCode.Trim();
        profile.City                   = cmd.City.Trim();
        profile.Country                = cmd.Country.Trim();
        profile.BusinessStatus         = cmd.BusinessStatus;
        profile.ActivityType           = cmd.ActivityType;
        profile.VatThresholdServices   = cmd.VatThresholdServices;
        profile.VatThresholdCommercial = cmd.VatThresholdCommercial;
        if (cmd.LogoPath is not null)
            profile.LogoPath = cmd.LogoPath;

        var newValues = JsonSerializer.Serialize(new
        {
            profile.LegalName, profile.Siret, profile.BusinessStatus,
            profile.ActivityType, profile.VatThresholdServices, profile.VatThresholdCommercial,
        });

        db.AuditLogs.Add(new AuditLog
        {
            EntityType = nameof(TenantProfile),
            RecordId   = cmd.TenantId.ToString(),
            Action     = "ProfileSaved",
            ChangedBy  = cmd.SavedByUserName,
            ChangedAt  = DateTimeOffset.UtcNow,
            OldValues  = oldValues,
            NewValues  = newValues,
        });

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static TenantProfileDto DefaultDto(Guid tenantId) =>
        new(tenantId, "", "", "", "", "", "France",
            BusinessStatus.Standard, ActivityType.Services,
            DefaultThresholdServices, DefaultThresholdCommercial, null);

    private static TenantProfileDto ToDto(TenantProfile p) =>
        new(p.TenantId, p.LegalName, p.Siret, p.AddressLine, p.PostalCode, p.City, p.Country,
            p.BusinessStatus, p.ActivityType,
            p.VatThresholdServices  ?? DefaultThresholdServices,
            p.VatThresholdCommercial ?? DefaultThresholdCommercial,
            p.LogoPath);
}
