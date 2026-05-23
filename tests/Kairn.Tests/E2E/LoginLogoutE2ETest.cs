using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Kairn.Tests.E2E;

[Collection("Integration")]
[Trait("Category", "E2E")]
public class LoginLogoutE2ETest : IAsyncLifetime
{
    private readonly E2EWebApplicationFactory _factory = new();
    private IPlaywright? _playwright;
    private IBrowser?   _browser;

    public async Task InitializeAsync()
    {
        _factory.CreateClient();
        _playwright = await Playwright.CreateAsync();
        _browser    = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Login_redirects_to_home_after_valid_credentials()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_factory.ServerUrl}/account/login?culture=en");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.FillAsync("#email",    E2EHelpers.AdminEmail);
        await page.FillAsync("#password", E2EHelpers.AdminPassword);
        await page.ClickAsync("button[type=submit]");

        await page.WaitForURLAsync(url => !url.Contains("/account/login"));
        page.Url.Should().NotContain("/account/login");
    }

    [Fact]
    public async Task Login_shows_error_for_wrong_password()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_factory.ServerUrl}/account/login?culture=en");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.FillAsync("#email",    E2EHelpers.AdminEmail);
        await page.FillAsync("#password", "wrongpassword");
        await page.ClickAsync("button[type=submit]");

        await page.WaitForSelectorAsync(".error");
        var errorText = await page.Locator(".error").InnerTextAsync();
        errorText.Should().Contain("Invalid email or password");
        page.Url.Should().Contain("/account/login");
    }

    [Fact]
    public async Task Logout_redirects_back_to_login_page()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await E2EHelpers.LoginAsync(page, _factory.ServerUrl);

        // Wait for Blazor circuit to activate the nav
        await page.WaitForSelectorAsync("a[href='/ledger']");

        // Submit the logout form rendered in MainLayout
        await page.Locator("form[action='/account/logout'] button[type=submit]").ClickAsync();

        await page.WaitForURLAsync(url => url.Contains("/account/login"));
        page.Url.Should().Contain("/account/login");
    }
}
