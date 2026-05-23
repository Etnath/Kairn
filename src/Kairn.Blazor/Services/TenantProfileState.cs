using Kairn.Application.Common;
using Kairn.Application.Features.CompanyProfile;
using Kairn.Domain.Entities;

namespace Kairn.Blazor.Services;

public class TenantProfileState(ITenantProfileService profileService, ICurrentUserContext currentUser)
{
    public BusinessStatus BusinessStatus    { get; private set; } = BusinessStatus.Standard;
    public bool           IsAutoEntrepreneur => BusinessStatus == BusinessStatus.AutoEntrepreneur;

    private bool _loaded;

    public event Action? OnChange;

    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_loaded) return;
        await RefreshAsync(ct);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var profile  = await profileService.GetAsync(currentUser.TenantId, ct);
        BusinessStatus = profile.BusinessStatus;
        _loaded      = true;
        OnChange?.Invoke();
    }

    public void Notify(BusinessStatus newStatus)
    {
        BusinessStatus = newStatus;
        _loaded        = true;
        OnChange?.Invoke();
    }
}
