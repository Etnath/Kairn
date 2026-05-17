namespace Kairn.Application.Features.Reports;

public interface ITrialBalanceService
{
    Task<TrialBalanceReport> GenerateAsync(Guid tenantId, DateOnly asOf, CancellationToken ct = default);
}
