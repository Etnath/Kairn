namespace Kairn.Application.Features.Dashboard;

public record UserDashboardPreferencesDto(
    bool ShowMonthlyRevenue,
    bool ShowMonthlyExpenses,
    bool ShowNetProfit,
    bool ShowOutstandingAr,
    bool ShowOutstandingAp,
    bool ShowCashBalance)
{
    public static UserDashboardPreferencesDto AllVisible =>
        new(true, true, true, true, true, true);
}

public record SaveUserDashboardPreferencesCommand(
    string UserId,
    bool ShowMonthlyRevenue,
    bool ShowMonthlyExpenses,
    bool ShowNetProfit,
    bool ShowOutstandingAr,
    bool ShowOutstandingAp,
    bool ShowCashBalance);

public interface IUserDashboardPreferencesService
{
    Task<UserDashboardPreferencesDto> GetAsync(string userId, CancellationToken ct = default);
    Task SaveAsync(SaveUserDashboardPreferencesCommand command, CancellationToken ct = default);
}
