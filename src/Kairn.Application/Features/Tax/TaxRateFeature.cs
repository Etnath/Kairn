using Kairn.Application.Common;
using Kairn.Domain.Entities;

namespace Kairn.Application.Features.Tax;

public record TaxRateDto(
    Guid       Id,
    string     Name,
    decimal    Rate,
    TaxCategory Category,
    bool       IsDefault,
    DateOnly   ValidFrom,
    DateOnly?  ValidTo,
    bool       IsActive);

public record CreateTaxRateCommand(
    Guid        TenantId,
    string      Name,
    decimal     Rate,
    TaxCategory Category,
    bool        IsDefault,
    DateOnly    ValidFrom,
    DateOnly?   ValidTo);

public record UpdateTaxRateCommand(
    Guid        Id,
    Guid        TenantId,
    string      Name,
    decimal     Rate,
    TaxCategory Category,
    bool        IsDefault,
    DateOnly    ValidFrom,
    DateOnly?   ValidTo);

public interface ITaxRateService
{
    Task<IReadOnlyList<TaxRateDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TaxRateDto>> GetActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task<TaxRateDto?> GetDefaultForDateAsync(Guid tenantId, TaxCategory category, DateOnly date, CancellationToken ct = default);
    Task<Result<TaxRateDto>> CreateAsync(CreateTaxRateCommand cmd, CancellationToken ct = default);
    Task<Result<TaxRateDto>> UpdateAsync(UpdateTaxRateCommand cmd, CancellationToken ct = default);
    Task<Result> SetActiveAsync(Guid id, Guid tenantId, bool isActive, CancellationToken ct = default);
    Task<bool> IsUsedInPostedTransactionAsync(Guid id, CancellationToken ct = default);
}
