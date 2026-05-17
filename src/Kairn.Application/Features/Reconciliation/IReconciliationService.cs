using Kairn.Application.Common;

namespace Kairn.Application.Features.Reconciliation;

public interface IReconciliationService
{
    Task<ReconciliationSessionDto> StartSessionAsync(StartReconciliationCommand cmd, CancellationToken ct = default);
    Task<Result> ImportLinesAsync(ImportLinesCommand cmd, CancellationToken ct = default);
    Task<ReconciliationSessionDto?> GetSessionAsync(Guid sessionId, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<LedgerLineForReconciliationDto>> GetUnreconciledLinesAsync(Guid tenantId, Guid accountId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Result> MatchAsync(MatchCommand cmd, CancellationToken ct = default);
    Task<Result> UnmatchAsync(Guid sessionId, Guid tenantId, Guid bankLineId, CancellationToken ct = default);
    Task<Result> CompleteAsync(Guid sessionId, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<ReconciliationSessionSummaryDto>> GetSessionsAsync(Guid tenantId, CancellationToken ct = default);
}
