using System.Text.RegularExpressions;
using Kairn.Application.Features.Reconciliation;

namespace Kairn.Infrastructure.Persistence.Services;

public class OfxParser : IOfxParser
{
    private static readonly Regex TagValue = new(@"<(\w+)>([^<\r\n]*)", RegexOptions.Compiled);

    public Task<IReadOnlyList<ParsedTransaction>> ParseAsync(Stream content, CancellationToken ct = default)
    {
        using var reader = new StreamReader(content);
        var text = reader.ReadToEnd();

        var transactions = new List<ParsedTransaction>();

        // Split into STMTTRN blocks
        var blocks = Regex.Split(text, @"</?STMTTRN>", RegexOptions.IgnoreCase);

        for (var i = 0; i < blocks.Length; i++)
        {
            var block = blocks[i];
            if (!block.Contains("DTPOSTED", StringComparison.OrdinalIgnoreCase) &&
                !block.Contains("TRNAMT", StringComparison.OrdinalIgnoreCase))
                continue;

            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in TagValue.Matches(block))
                fields[m.Groups[1].Value] = m.Groups[2].Value.Trim();

            if (!fields.TryGetValue("TRNAMT", out var amountStr) ||
                !decimal.TryParse(amountStr.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount))
                continue;

            if (!fields.TryGetValue("DTPOSTED", out var dateStr))
                continue;

            var date = ParseOfxDate(dateStr);
            if (date is null) continue;

            fields.TryGetValue("NAME", out var name);
            fields.TryGetValue("MEMO", out var memo);
            fields.TryGetValue("FITID", out var fitid);

            var description = !string.IsNullOrWhiteSpace(name) ? name : memo ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(memo) && memo != description)
                description = $"{description} – {memo}";

            transactions.Add(new ParsedTransaction(date.Value, description.Trim(), amount, fitid));
        }

        return Task.FromResult<IReadOnlyList<ParsedTransaction>>(transactions);
    }

    private static DateOnly? ParseOfxDate(string raw)
    {
        // Format: YYYYMMDD[HHMMSS[.XXX[ZZZ]]]
        if (raw.Length >= 8 &&
            int.TryParse(raw[..4], out var y) &&
            int.TryParse(raw[4..6], out var mo) &&
            int.TryParse(raw[6..8], out var d))
        {
            try { return new DateOnly(y, mo, d); }
            catch { }
        }
        return null;
    }
}
