using Kairn.Application.Common;

namespace Kairn.Application.Features.Audit;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetPagedAsync(AuditLogQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken ct = default);
}
