namespace Kairn.Application.Features.Audit;

public record AuditLogDto(
    long LogId,
    string EntityType,
    string RecordId,
    string Action,
    string ChangedBy,
    DateTimeOffset ChangedAt,
    string? OldValues,
    string? NewValues);

public record AuditLogQuery(
    string? EntityType = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? ChangedBy = null,
    int Page = 1,
    int PageSize = 50);
