using FluentAssertions;
using Kairn.Blazor.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System.Reflection;
using Xunit;

namespace Kairn.Tests;

public class LocalisationSmokeTest
{
    private static IStringLocalizer<NavMenu> BuildNavMenuLocalizer(string culture)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "Resources");
        var provider = services.BuildServiceProvider();

        var ci = new System.Globalization.CultureInfo(culture);
        System.Globalization.CultureInfo.CurrentUICulture = ci;
        System.Globalization.CultureInfo.CurrentCulture = ci;

        return provider.GetRequiredService<IStringLocalizer<NavMenu>>();
    }

    [Fact]
    public void NavMenu_localizer_resolves_without_throwing()
    {
        var localizer = BuildNavMenuLocalizer("en");
        var act = () => _ = localizer["Nav.Dashboard"];
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Nav.Dashboard",      "Dashboard")]
    [InlineData("Nav.GeneralLedger",  "General Ledger")]
    [InlineData("Nav.Invoicing",      "Invoicing")]
    [InlineData("Nav.Bills",          "Bills")]
    [InlineData("Nav.Reports",        "Reports")]
    [InlineData("Nav.MarginAnalysis", "Margin Analysis")]
    [InlineData("Nav.Tax",            "Tax")]
    [InlineData("Nav.Budgets",        "Budgets")]
    [InlineData("Nav.Settings",       "Settings")]
    public void NavMenu_en_returns_correct_english_label(string key, string expected)
    {
        var localizer = BuildNavMenuLocalizer("en");
        localizer[key].Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("Nav.Dashboard",      "Tableau de bord")]
    [InlineData("Nav.GeneralLedger",  "Grand livre")]
    [InlineData("Nav.Invoicing",      "Facturation")]
    [InlineData("Nav.Bills",          "Factures fournisseurs")]
    [InlineData("Nav.Reports",        "Rapports")]
    [InlineData("Nav.MarginAnalysis", "Analyse des marges")]
    [InlineData("Nav.Tax",            "TVA & Fiscalité")]
    [InlineData("Nav.Budgets",        "Budgets")]
    [InlineData("Nav.Settings",       "Paramètres")]
    public void NavMenu_fr_CH_returns_correct_french_label(string key, string expected)
    {
        var localizer = BuildNavMenuLocalizer("fr-FR");
        localizer[key].Value.Should().Be(expected);
    }

    [Fact]
    public void NavMenu_fr_CH_resolves_via_culture_hierarchy()
    {
        // fr-FR falls back to fr.resx — verify the lookup chain works
        var localizer = BuildNavMenuLocalizer("fr-FR");
        localizer["Nav.Dashboard"].ResourceNotFound.Should().BeFalse();
    }
}
