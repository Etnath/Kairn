using Fluxor;

namespace Kairn.Blazor.State;

[FeatureState]
public record DashboardState(bool IsLoading, string? Error)
{
    public DashboardState() : this(false, null) { }
}

public record LoadDashboardAction;
public record DashboardLoadedAction;
public record DashboardLoadFailedAction(string Error);

public static class DashboardReducers
{
    [ReducerMethod]
    public static DashboardState OnLoad(DashboardState state, LoadDashboardAction _) =>
        state with { IsLoading = true, Error = null };

    [ReducerMethod]
    public static DashboardState OnLoaded(DashboardState state, DashboardLoadedAction _) =>
        state with { IsLoading = false };

    [ReducerMethod]
    public static DashboardState OnFailed(DashboardState state, DashboardLoadFailedAction action) =>
        state with { IsLoading = false, Error = action.Error };
}
