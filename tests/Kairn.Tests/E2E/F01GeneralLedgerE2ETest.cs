using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Kairn.Tests.E2E;

/// <summary>
/// E2E tests for F01 General Ledger: Chart of Accounts (US1) and Journal Entries (US2/US3).
///
/// Navigation strategy: after login we wait for the Blazor circuit to connect (confirmed
/// by the nav link appearing), then navigate to target pages by CLICKING links inside the
/// running circuit. This avoids a full-page reload (GotoAsync) which would destroy the
/// existing circuit and force a new one to connect, making selectors unreliable.
/// </summary>
[Collection("Integration")]
[Trait("Category", "E2E")]
public class F01GeneralLedgerE2ETest : IAsyncLifetime
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

    /// <summary>
    /// Logs in, waits for the Blazor circuit to connect (nav visible),
    /// then clicks through to the target page inside the live circuit.
    /// </summary>
    private async Task<IPage> LoginAndNavigateAsync(IBrowserContext context, string navHref,
        string? secondHref = null)
    {
        var page = await context.NewPageAsync();

        // Login with English culture cookie
        await E2EHelpers.LoginAsync(page, _factory.ServerUrl);

        // Wait for the Blazor interactive circuit to be ready
        // (the nav menu appears only once the SignalR circuit has rendered MainLayout)
        await page.WaitForSelectorAsync("a[href='/ledger']");

        // Navigate within the live circuit — clicking is intercepted by blazor.web.js
        // and handled as an enhanced navigation (no circuit reset)
        await page.ClickAsync($"a[href='{navHref}']");
        await page.WaitForURLAsync(url => url.Contains(navHref));

        if (secondHref is not null)
        {
            await page.WaitForSelectorAsync($"a[href='{secondHref}']");
            await page.ClickAsync($"a[href='{secondHref}']");
            await page.WaitForURLAsync(url => url.Contains(secondHref));
        }

        return page;
    }

    // ── US1: Chart of Accounts ─────────────────────────────────────────────────

    [Fact]
    public async Task ChartOfAccounts_displays_seeded_accounts()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await LoginAndNavigateAsync(context,
            navHref:    "/settings",
            secondHref: "/settings/chart-of-accounts");

        await page.WaitForSelectorAsync("text=Capital social");

        var content = await page.ContentAsync();
        content.Should().Contain("Capital social",  "equity account from Swiss COA seed");
        content.Should().Contain("Clients",         "receivables account code 411000");
        content.Should().Contain("512000",          "main bank account code");
    }

    [Fact]
    public async Task ChartOfAccounts_can_create_a_new_account()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await LoginAndNavigateAsync(context,
            navHref:    "/settings",
            secondHref: "/settings/chart-of-accounts");

        await page.WaitForSelectorAsync("text=New Account");
        await page.ClickAsync("text=New Account");

        // Dialog opens — fill Code and Name (Type defaults to Asset, Currency to EUR)
        await page.WaitForSelectorAsync("[role='dialog']");
        await page.GetByLabel("Code").FillAsync("999001");
        await page.GetByLabel("Name").FillAsync("E2E Test Account");

        await page.ClickAsync("[role='dialog'] button:has-text('Save')");

        // Dialog closes and new row appears
        await page.WaitForSelectorAsync("text=999001");
        var content = await page.ContentAsync();
        content.Should().Contain("999001");
        content.Should().Contain("E2E Test Account");
    }

    [Fact]
    public async Task ChartOfAccounts_shows_error_for_duplicate_account_code()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await LoginAndNavigateAsync(context,
            navHref:    "/settings",
            secondHref: "/settings/chart-of-accounts");

        await page.WaitForSelectorAsync("text=New Account");
        await page.ClickAsync("text=New Account");
        await page.WaitForSelectorAsync("[role='dialog']");

        // Use an existing seed code to trigger the duplicate-code error
        await page.GetByLabel("Code").FillAsync("411000");
        await page.GetByLabel("Name").FillAsync("Duplicate test");

        await page.ClickAsync("[role='dialog'] button:has-text('Save')");

        await page.WaitForSelectorAsync("text=Account code already exists");
    }

    // ── US2: Journal Entries ───────────────────────────────────────────────────

    [Fact]
    public async Task GeneralLedger_page_loads_with_journal_entries_tab_and_new_entry_button()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await LoginAndNavigateAsync(context, navHref: "/ledger");

        await page.WaitForSelectorAsync("text=New Entry");

        var content = await page.ContentAsync();
        content.Should().Contain("General Ledger");
        content.Should().Contain("Journal Entries");
        content.Should().Contain("New Entry");
    }

    [Fact]
    public async Task GeneralLedger_new_entry_dialog_save_disabled_until_amounts_entered()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await LoginAndNavigateAsync(context, navHref: "/ledger");

        await page.WaitForSelectorAsync("text=New Entry");
        await page.ClickAsync("text=New Entry");

        await page.WaitForSelectorAsync("[role='dialog']");
        var dialog = page.Locator("[role='dialog']");

        // Save button is disabled — TotalBaseDebit is 0 when dialog first opens
        var saveBtn = dialog.Locator("button:has-text('Save')");
        (await saveBtn.IsDisabledAsync()).Should().BeTrue(
            "save must be disabled until the entry carries a non-zero balanced amount");
    }

    [Fact]
    public async Task GeneralLedger_can_save_a_balanced_journal_entry()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await LoginAndNavigateAsync(context, navHref: "/ledger");

        await page.WaitForSelectorAsync("text=New Entry");
        await page.ClickAsync("text=New Entry");
        await page.WaitForSelectorAsync("[role='dialog']");

        // Fill the description (scope to dialog to avoid matching the page-level search input)
        var dialog = page.Locator("[role='dialog']");
        await dialog.GetByLabel("Description").FillAsync("E2E balanced entry");

        // Line 1: account 706000 (Prestations de services), debit 100
        var accountInputs = page.Locator("[role='dialog'] .mud-autocomplete input");
        await accountInputs.First.FillAsync("706");
        await page.WaitForSelectorAsync(".mud-list-item");
        await page.Locator(".mud-list-item").First.ClickAsync();

        // Each row: Debit at Nth(0/2), Credit at Nth(1/3); Tab commits @bind-Value on change
        var numericInputs = page.Locator("[role='dialog'] input[inputmode='decimal']");
        await numericInputs.Nth(0).ClickAsync(new() { ClickCount = 3 });
        await numericInputs.Nth(0).FillAsync("100");
        await numericInputs.Nth(0).PressAsync("Tab");

        // Line 2: account 411000 (Clients), credit 100
        await accountInputs.Last.FillAsync("411");
        await page.WaitForSelectorAsync(".mud-list-item");
        await page.Locator(".mud-list-item").First.ClickAsync();

        await numericInputs.Nth(3).ClickAsync(new() { ClickCount = 3 });
        await numericInputs.Nth(3).FillAsync("100");
        await numericInputs.Nth(3).PressAsync("Tab");

        // Balance indicator turns green
        await page.WaitForSelectorAsync("text=Balanced");

        // Save becomes enabled
        var saveBtn = page.Locator("[role='dialog'] button:has-text('Save')");
        (await saveBtn.IsEnabledAsync()).Should().BeTrue();

        await saveBtn.ClickAsync();

        // Success snackbar confirms the creation
        await page.WaitForSelectorAsync(".mud-snackbar");
        var snackText = await page.Locator(".mud-snackbar").First.InnerTextAsync();
        snackText.Should().Contain("created");
    }

    // ── US3: Soft-delete / Admin controls ─────────────────────────────────────

    [Fact]
    public async Task GeneralLedger_show_deleted_toggle_is_visible_for_admin()
    {
        await using var context = await _browser!.NewContextAsync();
        var page = await LoginAndNavigateAsync(context, navHref: "/ledger");

        await page.WaitForSelectorAsync("text=Show deleted");
    }
}
