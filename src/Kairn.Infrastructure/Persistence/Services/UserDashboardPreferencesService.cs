using Kairn.Application.Features.Dashboard;
using Kairn.Domain.Entities;

namespace Kairn.Infrastructure.Persistence.Services;

public class UserDashboardPreferencesService(AppDbContext db) : IUserDashboardPreferencesService
{
    public async Task<UserDashboardPreferencesDto> GetAsync(string userId, CancellationToken ct = default)
    {
        var p = await db.UserDashboardPreferences.FindAsync([userId], ct);
        return p is null
            ? UserDashboardPreferencesDto.AllVisible
            : new UserDashboardPreferencesDto(
                p.ShowMonthlyRevenue, p.ShowMonthlyExpenses, p.ShowNetProfit,
                p.ShowOutstandingAr,  p.ShowOutstandingAp,  p.ShowCashBalance);
    }

    public async Task SaveAsync(SaveUserDashboardPreferencesCommand command, CancellationToken ct = default)
    {
        var p = await db.UserDashboardPreferences.FindAsync([command.UserId], ct);
        if (p is null)
        {
            db.UserDashboardPreferences.Add(new UserDashboardPreferences
            {
                UserId              = command.UserId,
                ShowMonthlyRevenue  = command.ShowMonthlyRevenue,
                ShowMonthlyExpenses = command.ShowMonthlyExpenses,
                ShowNetProfit       = command.ShowNetProfit,
                ShowOutstandingAr   = command.ShowOutstandingAr,
                ShowOutstandingAp   = command.ShowOutstandingAp,
                ShowCashBalance     = command.ShowCashBalance,
            });
        }
        else
        {
            p.ShowMonthlyRevenue  = command.ShowMonthlyRevenue;
            p.ShowMonthlyExpenses = command.ShowMonthlyExpenses;
            p.ShowNetProfit       = command.ShowNetProfit;
            p.ShowOutstandingAr   = command.ShowOutstandingAr;
            p.ShowOutstandingAp   = command.ShowOutstandingAp;
            p.ShowCashBalance     = command.ShowCashBalance;
        }
        await db.SaveChangesAsync(ct);
    }
}
