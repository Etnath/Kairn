using Fluxor;
using Kairn.Application.Features.Dashboard;
using Microsoft.Extensions.Logging;

namespace Kairn.Blazor.State;

[FeatureState]
public record DashboardState
{
    public bool IsLoading      { get; init; } = true;
    public bool HasError       { get; init; }
    public DashboardKpis? Kpis { get; init; }

    public bool IsChartsLoading    { get; init; } = true;
    public bool HasChartsError     { get; init; }
    public DashboardChartData? Charts { get; init; }
}

// KPI actions
public record LoadDashboardKpisAction(Guid TenantId);
public record DashboardKpisLoadedAction(DashboardKpis Kpis);
public record DashboardKpisFailedAction;

// Chart actions
public record LoadDashboardChartsAction(Guid TenantId);
public record DashboardChartsLoadedAction(DashboardChartData Charts);
public record DashboardChartsFailedAction;

public static class DashboardReducers
{
    [ReducerMethod(typeof(LoadDashboardKpisAction))]
    public static DashboardState OnKpisLoad(DashboardState state) =>
        state with { IsLoading = true, HasError = false };

    [ReducerMethod]
    public static DashboardState OnKpisLoaded(DashboardState state, DashboardKpisLoadedAction action) =>
        state with { IsLoading = false, HasError = false, Kpis = action.Kpis };

    [ReducerMethod(typeof(DashboardKpisFailedAction))]
    public static DashboardState OnKpisFailed(DashboardState state) =>
        state with { IsLoading = false, HasError = true };

    [ReducerMethod(typeof(LoadDashboardChartsAction))]
    public static DashboardState OnChartsLoad(DashboardState state) =>
        state with { IsChartsLoading = true, HasChartsError = false };

    [ReducerMethod]
    public static DashboardState OnChartsLoaded(DashboardState state, DashboardChartsLoadedAction action) =>
        state with { IsChartsLoading = false, HasChartsError = false, Charts = action.Charts };

    [ReducerMethod(typeof(DashboardChartsFailedAction))]
    public static DashboardState OnChartsFailed(DashboardState state) =>
        state with { IsChartsLoading = false, HasChartsError = true };
}

public class DashboardEffects(
    IDashboardService dashboardService,
    ILogger<DashboardEffects> logger)
{
    [EffectMethod]
    public async Task LoadKpisAsync(LoadDashboardKpisAction action, IDispatcher dispatcher)
    {
        try
        {
            var kpis = await dashboardService.GetKpisAsync(action.TenantId);
            dispatcher.Dispatch(new DashboardKpisLoadedAction(kpis));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load dashboard KPIs for tenant {TenantId}", action.TenantId);
            dispatcher.Dispatch(new DashboardKpisFailedAction());
        }
    }

    [EffectMethod]
    public async Task LoadChartsAsync(LoadDashboardChartsAction action, IDispatcher dispatcher)
    {
        try
        {
            var charts = await dashboardService.GetChartDataAsync(action.TenantId);
            dispatcher.Dispatch(new DashboardChartsLoadedAction(charts));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load dashboard charts for tenant {TenantId}", action.TenantId);
            dispatcher.Dispatch(new DashboardChartsFailedAction());
        }
    }
}
