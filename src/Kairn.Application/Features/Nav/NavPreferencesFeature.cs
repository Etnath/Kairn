namespace Kairn.Application.Features.Nav;

public interface IUserNavPreferencesService
{
    Task<HashSet<string>> GetCollapsedAsync(string userId, CancellationToken ct = default);
    Task SaveCollapsedAsync(string userId, IEnumerable<string> collapsed, CancellationToken ct = default);
}
