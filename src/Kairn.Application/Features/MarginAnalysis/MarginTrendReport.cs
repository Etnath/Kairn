namespace Kairn.Application.Features.MarginAnalysis;

public record MarginTrendPoint(DateOnly Month, decimal Revenue, decimal Cogs)
{
    public decimal  GrossProfit    => Revenue - Cogs;
    public decimal? GrossMarginPct => Revenue == 0m
        ? null
        : Math.Round((GrossProfit / Revenue) * 100m, 1);
}

public record MarginTrendSeries(
    Guid ProductLineId,
    string ProductLineName,
    IReadOnlyList<MarginTrendPoint> Points);

public record MarginTrendReport(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<MarginTrendSeries> Series);

public record MarginTrendQuery(Guid TenantId, DateOnly From, DateOnly To);

public interface IMarginTrendService
{
    Task<MarginTrendReport> GenerateAsync(MarginTrendQuery query, CancellationToken ct = default);
}
