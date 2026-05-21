namespace Kairn.Application.Features.Dashboard;

public record KpiSnapshot(decimal Current, decimal Prior)
{
    public decimal? DeltaPct => Prior == 0m
        ? null
        : Math.Round((Current - Prior) / Math.Abs(Prior) * 100m, 1);
}

public record DashboardKpis(
    KpiSnapshot MonthlyRevenue,
    KpiSnapshot MonthlyExpenses,
    KpiSnapshot NetProfit,
    KpiSnapshot OutstandingAr,
    KpiSnapshot OutstandingAp,
    KpiSnapshot CashBalance);

public record MonthlyPnlPoint(int Year, int Month, decimal Revenue, decimal Expenses)
{
    public decimal NetProfit => Revenue - Expenses;
}

public record MonthlyCashPoint(int Year, int Month, decimal Balance);

public record DashboardChartData(
    IReadOnlyList<MonthlyPnlPoint> MonthlyPnl,
    IReadOnlyList<MonthlyCashPoint> CashPosition);

public interface IDashboardService
{
    Task<DashboardKpis>      GetKpisAsync(Guid tenantId, CancellationToken ct = default);
    Task<DashboardChartData> GetChartDataAsync(Guid tenantId, CancellationToken ct = default);
}
