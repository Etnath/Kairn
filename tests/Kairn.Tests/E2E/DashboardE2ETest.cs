using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Kairn.Tests.E2E;

/// <summary>
/// E2E tests for the Dashboard page (/).
///
/// Regression coverage for two bugs:
/// 1. Missing Fluxor StoreInitializer — effects never ran because the Fluxor store was
///    never initialized (no <StoreInitializer /> in the layout). Fix: added it to MainLayout.
/// 2. DI captive dependency — DashboardEffects (singleton context) could not resolve
///    IDashboardService which previously depended on scoped AppDbContext. Fix: DashboardService
///    now owns its DbContext lifetime via IDbContextFactory, registered as singleton.
///
/// The tests verify that KPI cards actually render values rather than staying as skeletons.
/// The E2EHelpers.LoginAsync navigates to /account/login?culture=en which sets English
/// as the active culture for the session.
/// </summary>
[Collection("Integration")]
[Trait("Category", "E2E")]
public class DashboardE2ETest : IAsyncLifetime
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
    public async Task Dashboard_KpiCards_LoadAndDisplayValues()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await E2EHelpers.LoginAsync(page, _factory.ServerUrl);

        // Navigate to another page first so the Blazor interactive circuit is guaranteed
        // to be live before we land on the dashboard. This avoids relying on SSR-rendered
        // elements satisfying WaitForSelectorAsync before the circuit connects.
        await page.WaitForSelectorAsync("a[href='/ledger']");
        await page.ClickAsync("a[href='/ledger']");
        await page.WaitForURLAsync(url => url.Contains("/ledger"));

        // Navigate back to the dashboard inside the live circuit
        await page.ClickAsync("a[href='/']");
        await page.WaitForURLAsync(url => url.TrimEnd('/').EndsWith(_factory.ServerUrl.TrimEnd('/')));

        // Wait for KPI titles — they only render once the Fluxor effect completes and
        // DashboardKpisLoadedAction transitions IsLoading from true to false.
        // Login uses ?culture=en so the UI renders in English.
        await page.WaitForSelectorAsync("text=Revenue (this month)", new() { Timeout = 15_000 });

        (await page.IsVisibleAsync("text=Revenue (this month)")).Should().BeTrue();
        (await page.IsVisibleAsync("text=Expenses (this month)")).Should().BeTrue();
        (await page.IsVisibleAsync("text=Net Profit (this month)")).Should().BeTrue();
        (await page.IsVisibleAsync("text=Outstanding AR")).Should().BeTrue();
        (await page.IsVisibleAsync("text=Outstanding AP")).Should().BeTrue();

        var skeletons = await page.QuerySelectorAllAsync(".mud-skeleton");
        skeletons.Should().BeEmpty(because: "all KPI cards must have finished loading");
    }

    [Fact]
    public async Task Dashboard_KpiCards_ShowZeroForEmptyDatabase()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await E2EHelpers.LoginAsync(page, _factory.ServerUrl);

        await page.WaitForSelectorAsync("a[href='/ledger']");
        await page.ClickAsync("a[href='/ledger']");
        await page.WaitForURLAsync(url => url.Contains("/ledger"));
        await page.ClickAsync("a[href='/']");
        await page.WaitForURLAsync(url => url.TrimEnd('/').EndsWith(_factory.ServerUrl.TrimEnd('/')));

        await page.WaitForSelectorAsync("text=Revenue (this month)", new() { Timeout = 15_000 });

        // The E2E database has no journal entries — all KPIs should render as zero.
        // Use InnerTextAsync to get rendered text (avoids HTML entity encoding issues with €).
        var bodyText = await page.InnerTextAsync("body");
        bodyText.Should().Contain("0 €", because: "KPIs with no journal data should render as zero");
    }
}
