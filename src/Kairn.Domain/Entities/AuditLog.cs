namespace Kairn.Domain.Entities;

public class AuditLog
{
    public long LogId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}
