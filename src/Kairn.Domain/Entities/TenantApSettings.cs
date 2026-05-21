namespace Kairn.Domain.Entities;

public class TenantApSettings
{
    public Guid TenantId { get; set; }
    public bool ApprovalEnabled { get; set; }
    public decimal ApprovalThreshold { get; set; } = 1000m;
    public string ApproverRoles { get; set; } = "Admin";
}
