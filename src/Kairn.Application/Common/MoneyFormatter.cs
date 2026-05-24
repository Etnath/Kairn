using System.Globalization;

namespace Kairn.Application.Common;

public static class MoneyFormatter
{
    // ISO 4217 code → (symbol, placeBefore)
    // placeBefore = true  → "$ 1,234.56"   (Anglo-Saxon convention)
    // placeBefore = false → "1 234,56 €"   (French / continental convention)
    private static readonly Dictionary<string, (string Symbol, bool Before)> Map = new()
    {
        ["EUR"] = ("€",   false),
        ["USD"] = ("$",   true),
        ["GBP"] = ("£",   true),
        ["CHF"] = ("CHF", true),
        ["CAD"] = ("CA$", true),
    };

    /// <summary>
    /// Formats <paramref name="amount"/> with the given culture's number separators
    /// and places the currency symbol in its conventional position.
    /// Pass <c>null</c> to use <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    public static string Format(decimal amount, string currency, CultureInfo? culture = null)
    {
        var c = culture ?? CultureInfo.CurrentCulture;
        var number = amount.ToString("N2", c);

        if (Map.TryGetValue(currency.ToUpperInvariant(), out var info))
            return info.Before ? $"{info.Symbol} {number}" : $"{number} {info.Symbol}";

        return $"{number} {currency}";
    }
}
