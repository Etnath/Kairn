namespace Kairn.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Maps to PostgreSQL xmin for optimistic concurrency
    public uint RowVersion { get; set; }
}
