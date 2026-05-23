using Kairn.Domain.Entities;

namespace Kairn.Application.Features.Tenants;

public record CreateCompanyCommand(
    string            UserId,
    string            CompanyName,
    string?           LegalForm,
    string?           Siren,
    string?           AddressLine,
    string?           PostalCode,
    string?           City,
    BusinessStatus    BusinessStatus,
    ActivityType      ActivityType,
    int               FiscalYearStartMonth,
    VatFilingFrequency VatFilingFrequency);

public interface ICompanySetupService
{
    Task<Guid> CreateAsync(CreateCompanyCommand cmd, CancellationToken ct = default);
}
