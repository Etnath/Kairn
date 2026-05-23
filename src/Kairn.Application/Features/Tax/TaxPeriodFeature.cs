using Kairn.Application.Common;

namespace Kairn.Application.Features.Tax;

public record TaxPeriodDto(
    Guid           Id,
    string         Name,
    DateOnly       StartDate,
    DateOnly       EndDate,
    bool           IsLocked,
    string?        LockedByUserName,
    DateTimeOffset? LockedAt);

public record CreateTaxPeriodCommand(
    Guid     TenantId,
    string   Name,
    DateOnly StartDate,
    DateOnly EndDate);

public interface ITaxPeriodChecker
{
    Task<bool> IsDateLockedAsync(Guid tenantId, DateOnly date, CancellationToken ct = default);
}

public interface ITaxPeriodService : ITaxPeriodChecker
{
    Task<IReadOnlyList<TaxPeriodDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TaxPeriodDto>> GetOverlappingAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Result<TaxPeriodDto>> CreateAsync(CreateTaxPeriodCommand cmd, CancellationToken ct = default);
    Task<IReadOnlyList<TaxPeriodDto>> GenerateQuartersAsync(Guid tenantId, int year, CancellationToken ct = default);
    Task<Result<TaxPeriodDto>> LockAsync(Guid id, Guid tenantId, string userId, string userName, CancellationToken ct = default);
    Task<Result<TaxPeriodDto>> UnlockAsync(Guid id, Guid tenantId, string userId, string userName, string reason, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}
