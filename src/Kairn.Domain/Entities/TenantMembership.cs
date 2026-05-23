namespace Kairn.Domain.Entities;

public enum TenantRole { Owner, Admin, Member, ReadOnly }

public class TenantMembership
{
    public Guid             TenantId  { get; set; }
    public string           UserId    { get; set; } = "";
    public TenantRole       Role      { get; set; }
    public DateTimeOffset   JoinedAt  { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
