using Kairn.Application.Common;
using Microsoft.AspNetCore.Http;

namespace Kairn.Infrastructure.Persistence;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserContext(IHttpContextAccessor http) => _http = http;

    public string UserId =>
        _http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? "system";

    public string UserName =>
        _http.HttpContext?.User.Identity?.Name ?? "system";

    public Guid TenantId =>
        Guid.TryParse(
            _http.HttpContext?.User.FindFirst("tenant_id")?.Value, out var id)
        ? id
        : Guid.Empty;
}
