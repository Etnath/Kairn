using Kairn.Application.Features.Nav;
using Kairn.Domain.Entities;

namespace Kairn.Infrastructure.Persistence.Services;

public class UserNavPreferencesService(AppDbContext db) : IUserNavPreferencesService
{
    public async Task<HashSet<string>> GetCollapsedAsync(string userId, CancellationToken ct = default)
    {
        var p = await db.UserNavPreferences.FindAsync([userId], ct);
        if (p is null || string.IsNullOrWhiteSpace(p.CollapsedGroups))
            return [];
        return [.. p.CollapsedGroups.Split(';', StringSplitOptions.RemoveEmptyEntries)];
    }

    public async Task SaveCollapsedAsync(string userId, IEnumerable<string> collapsed, CancellationToken ct = default)
    {
        var value = string.Join(';', collapsed);
        var p = await db.UserNavPreferences.FindAsync([userId], ct);
        if (p is null)
            db.UserNavPreferences.Add(new UserNavPreferences { UserId = userId, CollapsedGroups = value });
        else
            p.CollapsedGroups = value;
        await db.SaveChangesAsync(ct);
    }
}
