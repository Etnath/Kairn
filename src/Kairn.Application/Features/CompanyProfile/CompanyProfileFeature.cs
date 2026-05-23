using Kairn.Application.Common;
using Kairn.Domain.Entities;

namespace Kairn.Application.Features.CompanyProfile;

public record TenantProfileDto(
    Guid           TenantId,
    string         LegalName,
    string         Siret,
    string         AddressLine,
    string         PostalCode,
    string         City,
    string         Country,
    BusinessStatus BusinessStatus,
    ActivityType   ActivityType,
    decimal        VatThresholdServices,
    decimal        VatThresholdCommercial,
    string?        LogoPath);

public record SaveTenantProfileCommand(
    Guid           TenantId,
    string         LegalName,
    string         Siret,
    string         AddressLine,
    string         PostalCode,
    string         City,
    string         Country,
    BusinessStatus BusinessStatus,
    ActivityType   ActivityType,
    decimal        VatThresholdServices,
    decimal        VatThresholdCommercial,
    string?        LogoPath,
    string         SavedByUserId,
    string         SavedByUserName);

public interface ITenantProfileService
{
    Task<TenantProfileDto> GetAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result> SaveAsync(SaveTenantProfileCommand cmd, CancellationToken ct = default);
}
