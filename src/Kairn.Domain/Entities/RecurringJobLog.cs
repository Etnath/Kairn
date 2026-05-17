namespace Kairn.Domain.Entities;

public class RecurringJobLog
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? RecurringEntryId { get; set; }
    public string EntryName { get; set; } = string.Empty;
    public DateTimeOffset AttemptedAt { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PostedReference { get; set; }
}
