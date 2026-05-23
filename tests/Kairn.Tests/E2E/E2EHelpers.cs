using Microsoft.Playwright;

namespace Kairn.Tests.E2E;

internal static class E2EHelpers
{
    internal const string AdminEmail    = "admin@kairn.local";
    internal const string AdminPassword = "Admin1234!";

    /// <summary>
    /// Navigates to the login page in English, fills credentials, submits, and waits
    /// until the browser leaves /account/login. The English culture cookie is set so
    /// subsequent Blazor pages are rendered in English.
    /// </summary>
    internal static async Task LoginAsync(IPage page, string serverUrl,
        string email    = AdminEmail,
        string password = AdminPassword)
    {
        await page.GotoAsync($"{serverUrl}/account/login?culture=en");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.FillAsync("#email", email);
        await page.FillAsync("#password", password);
        await page.ClickAsync("button[type=submit]");
        await page.WaitForURLAsync(url => !url.Contains("/account/login"));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
