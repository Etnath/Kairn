namespace Kairn.Domain.Entities;

public class UserDashboardPreferences
{
    public string UserId               { get; set; } = "";
    public bool   ShowMonthlyRevenue   { get; set; } = true;
    public bool   ShowMonthlyExpenses  { get; set; } = true;
    public bool   ShowNetProfit        { get; set; } = true;
    public bool   ShowOutstandingAr    { get; set; } = true;
    public bool   ShowOutstandingAp    { get; set; } = true;
    public bool   ShowCashBalance      { get; set; } = true;
}
