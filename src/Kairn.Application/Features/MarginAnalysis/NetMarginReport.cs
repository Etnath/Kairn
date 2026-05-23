namespace Kairn.Application.Features.MarginAnalysis;

public record NetMarginRow(
    Guid ProductLineId,
    string Name,
    decimal Revenue,
    decimal Cogs,
    decimal AllocatedOpEx,
    decimal? AlertThreshold)
{
    public decimal  GrossProfit    => Revenue - Cogs;
    public decimal? GrossMarginPct => Revenue == 0m
        ? null
        : Math.Round((GrossProfit / Revenue) * 100m, 1);
    public decimal  NetProfit      => GrossProfit - AllocatedOpEx;
    public decimal? NetMarginPct   => Revenue == 0m
        ? null
        : Math.Round((NetProfit / Revenue) * 100m, 1);
    public bool BelowThreshold     => AlertThreshold.HasValue && GrossMarginPct.HasValue
        && GrossMarginPct.Value < AlertThreshold.Value;
}

public record NetMarginReport(
    DateOnly From,
    DateOnly To,
    decimal TotalOpEx,
    bool AnyRulesConfigured,
    IReadOnlyList<NetMarginRow> Rows)
{
    public decimal  TotalRevenue     => Rows.Sum(r => r.Revenue);
    public decimal  TotalCogs        => Rows.Sum(r => r.Cogs);
    public decimal  TotalGrossProfit => TotalRevenue - TotalCogs;
    public decimal  TotalAllocated   => Rows.Sum(r => r.AllocatedOpEx);
    public decimal  TotalNetProfit   => Rows.Sum(r => r.NetProfit);
    public decimal? TotalNetMarginPct => TotalRevenue == 0m
        ? null
        : Math.Round((TotalNetProfit / TotalRevenue) * 100m, 1);
}

public interface INetMarginService
{
    Task<NetMarginReport> GenerateAsync(GrossMarginQuery query, CancellationToken ct = default);
}
