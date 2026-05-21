namespace Kairn.Domain.Entities;

public class TenantDashboardSettings
{
    public Guid    TenantId           { get; set; }
    public decimal CashAlertThreshold { get; set; } = 5_000m;
}
