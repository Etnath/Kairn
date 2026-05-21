namespace Kairn.Application.Features.Reports;

public record BsAccountLine(Guid AccountId, string Code, string Name, bool IsCurrent, decimal Balance);

public record BsReport(
    DateOnly AsOf,
    IReadOnlyList<BsAccountLine> AssetLines,
    IReadOnlyList<BsAccountLine> LiabilityLines,
    IReadOnlyList<BsAccountLine> EquityLines,
    decimal RetainedEarnings,
    decimal CurrentYearEarnings)
{
    public BsReport? Comparison { get; init; }
    public bool HasComparison => Comparison is not null;

    public decimal TotalCurrentAssets         => AssetLines.Where(l => l.IsCurrent).Sum(l => l.Balance);
    public decimal TotalNonCurrentAssets      => AssetLines.Where(l => !l.IsCurrent).Sum(l => l.Balance);
    public decimal TotalAssets                => TotalCurrentAssets + TotalNonCurrentAssets;
    public decimal TotalCurrentLiabilities    => LiabilityLines.Where(l => l.IsCurrent).Sum(l => l.Balance);
    public decimal TotalNonCurrentLiabilities => LiabilityLines.Where(l => !l.IsCurrent).Sum(l => l.Balance);
    public decimal TotalLiabilities           => TotalCurrentLiabilities + TotalNonCurrentLiabilities;
    public decimal TotalShareCapital          => EquityLines.Sum(l => l.Balance);
    public decimal TotalEquity                => TotalShareCapital + RetainedEarnings + CurrentYearEarnings;
    public bool    IsBalanced                 => Math.Abs(TotalAssets - (TotalLiabilities + TotalEquity)) < 0.01m;
}

public record BsExportMeta(string GeneratedBy, DateTimeOffset GeneratedAt)
{
    public static BsExportMeta Now(string userName) => new(userName, DateTimeOffset.UtcNow);
}

public record BsQuery(Guid TenantId, DateOnly AsOf, bool HideZero = false, DateOnly? CompareAsOf = null);

public interface IBsService
{
    Task<BsReport> GenerateAsync(BsQuery query, CancellationToken ct = default);
}

public interface IBsExporter
{
    byte[] ToPdf(BsReport report, BsExportMeta meta);
    string ToCsv(BsReport report, BsExportMeta meta);
}
