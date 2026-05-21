namespace Kairn.Domain.Entities;

public class DepreciationLog
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public DateOnly Period { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PostedReference { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
}
