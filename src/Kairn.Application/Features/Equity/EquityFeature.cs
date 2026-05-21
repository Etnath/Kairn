using Kairn.Application.Common;

namespace Kairn.Application.Features.Equity;

public record FiscalYearCloseDto(Guid Id, int FiscalYear, string ClosedByName, DateTimeOffset ClosedAt);

public record CloseYearCommand(Guid TenantId, int FiscalYear, string UserId, string UserName);

public interface IFiscalYearCloseService
{
    Task<IReadOnlyList<int>> GetClosedYearsAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<FiscalYearCloseDto>> CloseYearAsync(CloseYearCommand cmd, CancellationToken ct = default);
}
