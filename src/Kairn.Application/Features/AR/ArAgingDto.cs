namespace Kairn.Application.Features.AR;

public record ArAgingInvoiceDto(
    Guid Id,
    string Reference,
    DateOnly Date,
    DateOnly DueDate,
    int DaysOverdue,
    decimal Outstanding,
    string Bucket);

public record ArAgingRowDto(
    Guid CustomerId,
    string CustomerName,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days91Plus,
    decimal Total,
    IReadOnlyList<ArAgingInvoiceDto> Invoices);

public record ArAgingReportDto(DateOnly AsOf, IReadOnlyList<ArAgingRowDto> Rows)
{
    public decimal TotalCurrent    => Rows.Sum(r => r.Current);
    public decimal TotalDays1To30  => Rows.Sum(r => r.Days1To30);
    public decimal TotalDays31To60 => Rows.Sum(r => r.Days31To60);
    public decimal TotalDays61To90 => Rows.Sum(r => r.Days61To90);
    public decimal TotalDays91Plus => Rows.Sum(r => r.Days91Plus);
    public decimal GrandTotal      => Rows.Sum(r => r.Total);
}

public record ArAgingQuery(
    Guid TenantId,
    DateOnly AsOf,
    string? CustomerFilter = null,
    bool IncludeZeroBalance = false);

public interface IArAgingService
{
    Task<ArAgingReportDto> GenerateAsync(ArAgingQuery query, CancellationToken ct = default);
}

public interface IArAgingExporter
{
    byte[] ToPdf(ArAgingReportDto report);
    string ToCsv(ArAgingReportDto report);
}
