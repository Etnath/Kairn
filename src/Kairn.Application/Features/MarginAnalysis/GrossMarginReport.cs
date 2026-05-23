namespace Kairn.Application.Features.MarginAnalysis;

public record GrossMarginRow(
    Guid ProductLineId,
    string Name,
    decimal Revenue,
    decimal Cogs,
    decimal? AlertThreshold)
{
    public decimal  GrossProfit    => Revenue - Cogs;
    public decimal? GrossMarginPct => Revenue == 0m
        ? null
        : Math.Round((GrossProfit / Revenue) * 100m, 1);
    public bool BelowThreshold     => AlertThreshold.HasValue && GrossMarginPct.HasValue
        && GrossMarginPct.Value < AlertThreshold.Value;
}

public record GrossMarginReport(DateOnly From, DateOnly To, IReadOnlyList<GrossMarginRow> Rows)
{
    public decimal  TotalRevenue     => Rows.Sum(r => r.Revenue);
    public decimal  TotalCogs        => Rows.Sum(r => r.Cogs);
    public decimal  TotalGrossProfit => TotalRevenue - TotalCogs;
    public decimal? TotalMarginPct   => TotalRevenue == 0m
        ? null
        : Math.Round((TotalGrossProfit / TotalRevenue) * 100m, 1);
}

public record GrossMarginQuery(Guid TenantId, DateOnly From, DateOnly To);

public record GrossMarginExportMeta(string GeneratedBy, DateTimeOffset GeneratedAt)
{
    public static GrossMarginExportMeta Now(string userName) => new(userName, DateTimeOffset.UtcNow);
}

public interface IGrossMarginService
{
    Task<GrossMarginReport> GenerateAsync(GrossMarginQuery query, CancellationToken ct = default);
}

public interface IGrossMarginExporter
{
    byte[] ToPdf(GrossMarginReport report, GrossMarginExportMeta meta);
    string ToCsv(GrossMarginReport report, GrossMarginExportMeta meta);
    byte[] ToPdf(NetMarginReport report, GrossMarginExportMeta meta);
    string ToCsv(NetMarginReport report, GrossMarginExportMeta meta);
}
