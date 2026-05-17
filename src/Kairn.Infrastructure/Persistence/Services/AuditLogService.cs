using Kairn.Application.Common;
using Kairn.Application.Features.Audit;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class AuditLogService(AppDbContext db) : IAuditLogService
{
    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var q = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            q = q.Where(a => a.EntityType == query.EntityType);

        if (query.From.HasValue)
            q = q.Where(a => a.ChangedAt >= query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        if (query.To.HasValue)
            q = q.Where(a => a.ChangedAt < query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        if (!string.IsNullOrWhiteSpace(query.ChangedBy))
        {
            var cb = query.ChangedBy.Trim().ToLower();
            q = q.Where(a => a.ChangedBy.ToLower().Contains(cb));
        }

        q = q.OrderByDescending(a => a.ChangedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(a => new AuditLogDto(
            a.LogId, a.EntityType, a.RecordId, a.Action,
            a.ChangedBy, a.ChangedAt, a.OldValues, a.NewValues))
            .ToList();

        return new PagedResult<AuditLogDto>(dtos, total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken ct = default) =>
        await db.AuditLogs
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);
}
