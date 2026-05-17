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

        if (!string.IsNullOrWhiteSpace(query.ChangedBy))
        {
            var cb = query.ChangedBy.Trim().ToLower();
            q = q.Where(a => a.ChangedBy.ToLower().Contains(cb));
        }

        q = q.OrderByDescending(a => a.LogId);

        // SQLite cannot translate DateTimeOffset comparisons; materialise the
        // translatable results first, then apply the date window in-process.
        var rows = await q.ToListAsync(ct);

        if (query.From.HasValue)
        {
            var from = query.From.Value;
            rows = rows.Where(a => DateOnly.FromDateTime(a.ChangedAt.UtcDateTime) >= from).ToList();
        }

        if (query.To.HasValue)
        {
            var to = query.To.Value;
            rows = rows.Where(a => DateOnly.FromDateTime(a.ChangedAt.UtcDateTime) <= to).ToList();
        }

        var total = rows.Count;
        var items = rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditLogDto(
                a.LogId, a.EntityType, a.RecordId, a.Action,
                a.ChangedBy, a.ChangedAt, a.OldValues, a.NewValues))
            .ToList();

        return new PagedResult<AuditLogDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken ct = default) =>
        await db.AuditLogs
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);
}
