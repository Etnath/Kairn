using FluentAssertions;
using Kairn.Blazor.Shared;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Xunit;

namespace Kairn.Tests;

public class LocalisationCompletenessTest
{
    private static readonly Assembly BlazorAssembly = typeof(NavMenu).Assembly;

    // Every resource base name that must have parity between EN and FR
    private static readonly string[] ResourceBaseNames =
    [
        "Kairn.Blazor.Resources.Shared.NavMenu",
        "Kairn.Blazor.Resources.Shared.MainLayout",
        "Kairn.Blazor.Resources.Pages.Dashboard",
        "Kairn.Blazor.Resources.Pages.GeneralLedger",
        "Kairn.Blazor.Resources.Pages.Invoicing",
        "Kairn.Blazor.Resources.Pages.Bills",
        "Kairn.Blazor.Resources.Pages.Reports",
        "Kairn.Blazor.Resources.Pages.MarginAnalysis",
        "Kairn.Blazor.Resources.Pages.Tax",
        "Kairn.Blazor.Resources.Pages.Budgets",
        "Kairn.Blazor.Resources.Pages.Settings",
        "Kairn.Blazor.Resources.Pages.Coa.ChartOfAccounts",
        "Kairn.Blazor.Resources.Pages.AuditTrail.AuditLog",
        "Kairn.Blazor.Resources.Pages.GL.Reconciliation",
        "Kairn.Blazor.Resources.Pages.FinancialReports.TrialBalance",
        "Kairn.Blazor.Resources.Pages.GL.RecurringEntryDialog",
        "Kairn.Blazor.Resources.Pages.Account.LoginModel",
    ];

    public static IEnumerable<object[]> ResourceNames() =>
        ResourceBaseNames.Select(n => new object[] { n });

    [Theory]
    [MemberData(nameof(ResourceNames))]
    public void FR_resource_has_all_keys_present_in_EN(string baseName)
    {
        var rm = new ResourceManager(baseName, BlazorAssembly);

        var enKeys = LoadKeys(rm, "en");
        var frKeys = LoadKeys(rm, "fr-FR");

        enKeys.Should().NotBeEmpty(because: $"{baseName}.en.resx should have at least one entry");

        var missing = enKeys.Except(frKeys).ToList();
        missing.Should().BeEmpty(
            because: $"all EN keys in '{baseName}' should also exist in FR; missing: {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(ResourceNames))]
    public void EN_resource_has_no_empty_values(string baseName)
    {
        var rm = new ResourceManager(baseName, BlazorAssembly);
        var rs = rm.GetResourceSet(new CultureInfo("en"), createIfNotExists: true, tryParents: true);

        rs.Should().NotBeNull(because: $"{baseName} EN resource set should exist");

        foreach (DictionaryEntry entry in rs!)
        {
            ((string?)entry.Value).Should().NotBeNullOrWhiteSpace(
                because: $"key '{entry.Key}' in '{baseName}' EN should have a value");
        }
    }

    [Theory]
    [MemberData(nameof(ResourceNames))]
    public void FR_resource_has_no_empty_values(string baseName)
    {
        var rm = new ResourceManager(baseName, BlazorAssembly);
        var rs = rm.GetResourceSet(new CultureInfo("fr-FR"), createIfNotExists: true, tryParents: true);

        rs.Should().NotBeNull(because: $"{baseName} FR resource set should exist");

        foreach (DictionaryEntry entry in rs!)
        {
            ((string?)entry.Value).Should().NotBeNullOrWhiteSpace(
                because: $"key '{entry.Key}' in '{baseName}' FR should have a value");
        }
    }

    private static HashSet<string> LoadKeys(ResourceManager rm, string culture)
    {
        var rs = rm.GetResourceSet(new CultureInfo(culture), createIfNotExists: true, tryParents: true);
        if (rs is null) return [];
        return rs.Cast<DictionaryEntry>().Select(e => (string)e.Key).ToHashSet();
    }
}
