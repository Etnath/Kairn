namespace Kairn.Application.Features.AP;

public record ApAgingBillDto(
    Guid Id,
    string Reference,
    DateOnly Date,
    DateOnly DueDate,
    int DaysOverdue,
    decimal Outstanding,
    string Bucket);

public record ApAgingRowDto(
    Guid VendorId,
    string VendorName,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days91Plus,
    decimal Total,
    IReadOnlyList<ApAgingBillDto> Bills);

public record ApAgingReportDto(DateOnly AsOf, IReadOnlyList<ApAgingRowDto> Rows)
{
    public decimal TotalCurrent    => Rows.Sum(r => r.Current);
    public decimal TotalDays1To30  => Rows.Sum(r => r.Days1To30);
    public decimal TotalDays31To60 => Rows.Sum(r => r.Days31To60);
    public decimal TotalDays61To90 => Rows.Sum(r => r.Days61To90);
    public decimal TotalDays91Plus => Rows.Sum(r => r.Days91Plus);
    public decimal GrandTotal      => Rows.Sum(r => r.Total);
}

public record ApAgingQuery(
    Guid TenantId,
    DateOnly AsOf,
    string? VendorFilter = null,
    bool IncludeZeroBalance = false);

public interface IApAgingService
{
    Task<ApAgingReportDto> GenerateAsync(ApAgingQuery query, CancellationToken ct = default);
}

public interface IApAgingExporter
{
    byte[] ToPdf(ApAgingReportDto report);
    string ToCsv(ApAgingReportDto report);
}
