using Kairn.Application.Features.Reconciliation;

namespace Kairn.Infrastructure.Persistence.Services;

public class CsvParser : ICsvParser
{
    public async Task<(string[] Headers, string[][] Rows)> PreviewAsync(Stream content, string delimiter, CancellationToken ct = default)
    {
        using var reader = new StreamReader(content);
        var lines = new List<string[]>();
        string? line;
        while (lines.Count < 6 && (line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(SplitLine(line, delimiter));
        }

        if (lines.Count == 0) return ([], []);
        var headers = lines[0];
        var rows = lines.Skip(1).ToArray();
        return (headers, rows);
    }

    public async Task<IReadOnlyList<ParsedTransaction>> ParseAsync(Stream content, CsvColumnMapping mapping, CancellationToken ct = default)
    {
        using var reader = new StreamReader(content);
        var results = new List<ParsedTransaction>();
        string? line;
        var lineIndex = 0;

        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            lineIndex++;
            if (mapping.HasHeaderRow && lineIndex == 1) continue;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = SplitLine(line, mapping.Delimiter);

            var date = ParseDate(SafeGet(cols, mapping.DateColumnIndex), mapping.DateFormat);
            if (date is null) continue;

            var description = SafeGet(cols, mapping.DescriptionColumnIndex);

            decimal amount;
            if (mapping.DebitColumnIndex.HasValue && mapping.CreditColumnIndex.HasValue)
            {
                var debit  = ParseDecimal(SafeGet(cols, mapping.DebitColumnIndex.Value));
                var credit = ParseDecimal(SafeGet(cols, mapping.CreditColumnIndex.Value));
                // convention: money out = negative, money in = positive
                amount = credit - debit;
            }
            else
            {
                amount = ParseDecimal(SafeGet(cols, mapping.AmountColumnIndex));
            }

            results.Add(new ParsedTransaction(date.Value, description.Trim(), amount, null));
        }

        return results;
    }

    private static string SafeGet(string[] cols, int index) =>
        index >= 0 && index < cols.Length ? cols[index].Trim() : string.Empty;

    private static DateOnly? ParseDate(string raw, string format)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (DateOnly.TryParseExact(raw, format,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d))
            return d;
        if (DateOnly.TryParse(raw, out d)) return d;
        return null;
    }

    private static decimal ParseDecimal(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0m;
        // Accept both . and , as decimal separator
        var cleaned = raw.Replace(" ", "").Replace("'", "");
        if (cleaned.Contains(',') && cleaned.Contains('.'))
            cleaned = cleaned.Replace(",", "");      // 1,234.56 → 1234.56
        else
            cleaned = cleaned.Replace(',', '.');     // 1234,56 → 1234.56

        return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0m;
    }

    private static string[] SplitLine(string line, string delimiter)
    {
        var result = new List<string>();
        var sb = new System.Text.StringBuilder();
        var inQuote = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuote && i + 1 < line.Length && line[i + 1] == '"')
                { sb.Append('"'); i++; }
                else
                    inQuote = !inQuote;
            }
            else if (!inQuote && line[i..].StartsWith(delimiter))
            {
                result.Add(sb.ToString());
                sb.Clear();
                i += delimiter.Length - 1;
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }
}
