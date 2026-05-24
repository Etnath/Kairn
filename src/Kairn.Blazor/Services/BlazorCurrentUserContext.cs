using System.Security.Claims;
using Kairn.Application.Common;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Kairn.Blazor.Services;

/// <summary>
/// Blazor Server-safe implementation of ICurrentUserContext.
/// IHttpContextAccessor.HttpContext is null during the SignalR circuit phase,
/// so we fall back to AuthenticationStateProvider which is always available.
/// </summary>
public class BlazorCurrentUserContext(
    IHttpContextAccessor http,
    AuthenticationStateProvider authProvider) : ICurrentUserContext
{
    private ClaimsPrincipal GetUser()
    {
        if (http.HttpContext is { } ctx)
            return ctx.User;
        try
        {
            // Blazor Server SignalR circuit — HttpContext is null; auth state is cached (sync-safe)
            return authProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult().User;
        }
        catch
        {
            // Outside a Blazor circuit (e.g. seeding at startup) — treat as anonymous/system
            return new ClaimsPrincipal();
        }
    }

    public string UserId =>
        GetUser().FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

    public string UserName =>
        GetUser().Identity?.Name ?? "system";

    public Guid TenantId =>
        Guid.TryParse(GetUser().FindFirst("tenant_id")?.Value, out var id) ? id : Guid.Empty;
}
