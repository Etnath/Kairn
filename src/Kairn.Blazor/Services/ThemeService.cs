using Microsoft.AspNetCore.Http;

namespace Kairn.Blazor.Services;

public class ThemeService
{
    private const string CookieName = "theme";
    private readonly IHttpContextAccessor _http;

    public ThemeService(IHttpContextAccessor http)
    {
        _http = http;
        IsDarkMode = http.HttpContext?.Request.Cookies[CookieName] == "dark";
    }

    public bool IsDarkMode { get; private set; }

    public event Action? OnChange;

    public void Toggle()
    {
        IsDarkMode = !IsDarkMode;
        var response = _http.HttpContext?.Response;
        if (response is not null)
        {
            response.Cookies.Append(CookieName, IsDarkMode ? "dark" : "light",
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(365), SameSite = SameSiteMode.Lax });
        }
        OnChange?.Invoke();
    }
}
