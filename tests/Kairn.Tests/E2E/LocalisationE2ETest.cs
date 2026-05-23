using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Kairn.Tests.E2E;

[Collection("Integration")]
[Trait("Category", "E2E")]
public class LocalisationE2ETest : IAsyncLifetime
{
    private readonly E2EWebApplicationFactory _factory = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        _factory.CreateClient(); // triggers both hosts to start
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Login_page_renders_in_French_after_language_switch()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_factory.ServerUrl}/account/login");
        await page.ClickAsync("a[href*='culture=fr-FR']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var subtitle = await page.Locator(".subtitle").InnerTextAsync();
        subtitle.Should().Contain("Connectez-vous");

        var submitBtn = await page.Locator("button[type=submit]").InnerTextAsync();
        submitBtn.Trim().Should().Be("Se connecter");
    }

    [Fact]
    public async Task Nav_shows_Grand_livre_after_login_in_French()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        // Switch to French on the login page
        await page.GotoAsync($"{_factory.ServerUrl}/account/login");
        await page.ClickAsync("a[href*='culture=fr-FR']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Log in with the dev admin account
        await page.FillAsync("#email", "admin@kairn.local");
        await page.FillAsync("#password", "Admin1234!");
        await page.ClickAsync("button[type=submit]");

        // Wait for navigation to the authenticated home page
        await page.WaitForURLAsync(url => !url.Contains("/account/login"));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Blazor Server renders the nav via SignalR — wait for the ledger link in French
        await page.WaitForSelectorAsync("a[href='/ledger']");
        var linkText = await page.Locator("a[href='/ledger']").InnerTextAsync();
        linkText.Trim().Should().Contain("Grand livre");
    }
}
