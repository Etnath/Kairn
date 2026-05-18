using System.Globalization;

namespace Kairn.Blazor.Helpers;

public static class CultureHelper
{
    public static string FormatCurrency(decimal amount, string currencyCode = "EUR")
    {
        var culture = CultureInfo.CurrentUICulture;
        var formatted = amount.ToString("N2", culture);
        return $"{currencyCode} {formatted}";
    }

    public static string FormatDate(DateOnly date) =>
        date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentUICulture);

    public static string FormatDate(DateTimeOffset date) =>
        date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentUICulture);

    public static string FormatPercent(decimal value) =>
        value.ToString("N2", CultureInfo.CurrentUICulture) + " %";
}
