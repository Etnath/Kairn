namespace Kairn.Application.Features.Reconciliation;

public interface IOfxParser
{
    Task<IReadOnlyList<ParsedTransaction>> ParseAsync(Stream content, CancellationToken ct = default);
}

public interface ICsvParser
{
    Task<(string[] Headers, string[][] Rows)> PreviewAsync(Stream content, string delimiter, CancellationToken ct = default);
    Task<IReadOnlyList<ParsedTransaction>> ParseAsync(Stream content, CsvColumnMapping mapping, CancellationToken ct = default);
}
