namespace Kairn.Application.Features.Reports;

public enum PnlGroup { Revenue, Cogs, OperatingExpenses, Depreciation, Interest, Tax }

public record PnlAccountLine(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    PnlGroup Group,
    decimal Amount);

public record PnlReport(DateOnly From, DateOnly To, IReadOnlyList<PnlAccountLine> Lines)
{
    public decimal TotalRevenue           => Lines.Where(l => l.Group == PnlGroup.Revenue).Sum(l => l.Amount);
    public decimal TotalCogs              => Lines.Where(l => l.Group == PnlGroup.Cogs).Sum(l => l.Amount);
    public decimal GrossProfit            => TotalRevenue - TotalCogs;
    public decimal TotalOperatingExpenses => Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).Sum(l => l.Amount);
    public decimal TotalDepreciation      => Lines.Where(l => l.Group == PnlGroup.Depreciation).Sum(l => l.Amount);
    // EBITDA = Gross Profit − Operating Expenses + Depreciation & Amortisation
    public decimal Ebitda                 => GrossProfit - TotalOperatingExpenses + TotalDepreciation;
    public decimal TotalInterest          => Lines.Where(l => l.Group == PnlGroup.Interest).Sum(l => l.Amount);
    public decimal TotalTax               => Lines.Where(l => l.Group == PnlGroup.Tax).Sum(l => l.Amount);
    public decimal NetIncome              => Ebitda - TotalInterest - TotalTax;
}

public record PnlComparisonLine(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    PnlGroup Group,
    decimal Current,
    decimal Comparison)
{
    public decimal  Variance    => Current - Comparison;
    public decimal? VariancePct => Comparison == 0m
        ? null
        : Math.Round((Variance / Math.Abs(Comparison)) * 100m, 1);
}

public record PnlComparisonReport(PnlReport Current, PnlReport Comparison)
{
    public IReadOnlyList<PnlComparisonLine> Lines { get; } = BuildLines(Current, Comparison);

    private static IReadOnlyList<PnlComparisonLine> BuildLines(PnlReport cur, PnlReport comp)
    {
        var curDict  = cur.Lines.ToDictionary(l => l.AccountId);
        var compDict = comp.Lines.ToDictionary(l => l.AccountId);
        var result   = new List<PnlComparisonLine>();

        foreach (var l in cur.Lines)
        {
            var c = compDict.GetValueOrDefault(l.AccountId);
            result.Add(new PnlComparisonLine(l.AccountId, l.AccountCode, l.AccountName, l.Group,
                l.Amount, c?.Amount ?? 0m));
        }
        foreach (var l in comp.Lines.Where(l => !curDict.ContainsKey(l.AccountId)))
            result.Add(new PnlComparisonLine(l.AccountId, l.AccountCode, l.AccountName, l.Group,
                0m, l.Amount));

        return result.OrderBy(l => l.Group).ThenBy(l => l.AccountCode).ToList();
    }

    private decimal SumCur(PnlGroup g)  => Lines.Where(l => l.Group == g).Sum(l => l.Current);
    private decimal SumComp(PnlGroup g) => Lines.Where(l => l.Group == g).Sum(l => l.Comparison);

    public decimal CurRevenue   => SumCur(PnlGroup.Revenue);
    public decimal CompRevenue  => SumComp(PnlGroup.Revenue);
    public decimal CurCogs      => SumCur(PnlGroup.Cogs);
    public decimal CompCogs     => SumComp(PnlGroup.Cogs);
    public decimal CurGross     => CurRevenue - CurCogs;
    public decimal CompGross    => CompRevenue - CompCogs;
    public decimal CurOpEx      => Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).Sum(l => l.Current);
    public decimal CompOpEx     => Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).Sum(l => l.Comparison);
    public decimal CurDepr      => SumCur(PnlGroup.Depreciation);
    public decimal CompDepr     => SumComp(PnlGroup.Depreciation);
    public decimal CurEbitda    => CurGross - CurOpEx + CurDepr;
    public decimal CompEbitda   => CompGross - CompOpEx + CompDepr;
    public decimal CurInterest  => SumCur(PnlGroup.Interest);
    public decimal CompInterest => SumComp(PnlGroup.Interest);
    public decimal CurTax       => SumCur(PnlGroup.Tax);
    public decimal CompTax      => SumComp(PnlGroup.Tax);
    public decimal CurNet       => CurEbitda - CurInterest - CurTax;
    public decimal CompNet      => CompEbitda - CompInterest - CompTax;
}

public record PnlBudgetLine(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    PnlGroup Group,
    decimal Actual,
    decimal Budget)
{
    public decimal  Variance    => Actual - Budget;
    public decimal? VariancePct => Budget == 0m
        ? null
        : Math.Round((Variance / Math.Abs(Budget)) * 100m, 1);
}

public record PnlBudgetReport(PnlReport Actual, IReadOnlyList<PnlBudgetLine> Lines)
{
    private decimal SumAct(PnlGroup g) => Lines.Where(l => l.Group == g).Sum(l => l.Actual);
    private decimal SumBud(PnlGroup g) => Lines.Where(l => l.Group == g).Sum(l => l.Budget);

    public decimal ActRevenue  => SumAct(PnlGroup.Revenue);
    public decimal BudRevenue  => SumBud(PnlGroup.Revenue);
    public decimal ActCogs     => SumAct(PnlGroup.Cogs);
    public decimal BudCogs     => SumBud(PnlGroup.Cogs);
    public decimal ActGross    => ActRevenue - ActCogs;
    public decimal BudGross    => BudRevenue - BudCogs;
    public decimal ActOpEx     => Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).Sum(l => l.Actual);
    public decimal BudOpEx     => Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).Sum(l => l.Budget);
    public decimal ActDepr     => SumAct(PnlGroup.Depreciation);
    public decimal BudDepr     => SumBud(PnlGroup.Depreciation);
    public decimal ActEbitda   => ActGross - ActOpEx + ActDepr;
    public decimal BudEbitda   => BudGross - BudOpEx + BudDepr;
    public decimal ActInterest => SumAct(PnlGroup.Interest);
    public decimal BudInterest => SumBud(PnlGroup.Interest);
    public decimal ActTax      => SumAct(PnlGroup.Tax);
    public decimal BudTax      => SumBud(PnlGroup.Tax);
    public decimal ActNet      => ActEbitda - ActInterest - ActTax;
    public decimal BudNet      => BudEbitda - BudInterest - BudTax;
}

public record PnlDrillDownLine(
    Guid EntryId,
    DateOnly Date,
    string Reference,
    string EntryDescription,
    decimal Amount,
    string? Memo);

public record PnlExportMeta(string GeneratedBy, DateTimeOffset GeneratedAt)
{
    public static PnlExportMeta Now(string userName) => new(userName, DateTimeOffset.UtcNow);
}

public record PnlQuery(Guid TenantId, DateOnly From, DateOnly To, bool HideZero = true);

public record ForecastLine(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    PnlGroup Group,
    decimal YtdActual,
    decimal RemainingBudget,
    decimal AnnualBudget)
{
    public decimal  FullYearForecast    => YtdActual + RemainingBudget;
    public decimal  ForecastVsBudget    => FullYearForecast - AnnualBudget;
    public decimal? ForecastVsBudgetPct =>
        AnnualBudget == 0m ? null
        : Math.Round((ForecastVsBudget / Math.Abs(AnnualBudget)) * 100m, 1);
}

public record YearEndForecastReport(
    int FiscalYear,
    int ActualThroughMonth,
    IReadOnlyList<ForecastLine> Lines)
{
    private decimal SumYtd(PnlGroup g)  => Lines.Where(l => l.Group == g).Sum(l => l.YtdActual);
    private decimal SumBud(PnlGroup g)  => Lines.Where(l => l.Group == g).Sum(l => l.AnnualBudget);
    private decimal SumFcst(PnlGroup g) => Lines.Where(l => l.Group == g).Sum(l => l.FullYearForecast);

    public decimal YtdRevenue   => SumYtd(PnlGroup.Revenue);
    public decimal BudRevenue   => SumBud(PnlGroup.Revenue);
    public decimal FcstRevenue  => SumFcst(PnlGroup.Revenue);

    public decimal YtdCogs      => SumYtd(PnlGroup.Cogs);
    public decimal BudCogs      => SumBud(PnlGroup.Cogs);
    public decimal FcstCogs     => SumFcst(PnlGroup.Cogs);

    public decimal YtdGross     => YtdRevenue - YtdCogs;
    public decimal BudGross     => BudRevenue - BudCogs;
    public decimal FcstGross    => FcstRevenue - FcstCogs;

    public decimal YtdOpEx      => Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).Sum(l => l.YtdActual);
    public decimal BudOpEx      => Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).Sum(l => l.AnnualBudget);
    public decimal FcstOpEx     => Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).Sum(l => l.FullYearForecast);

    public decimal YtdDepr      => SumYtd(PnlGroup.Depreciation);
    public decimal BudDepr      => SumBud(PnlGroup.Depreciation);
    public decimal FcstDepr     => SumFcst(PnlGroup.Depreciation);

    public decimal YtdEbitda    => YtdGross  - YtdOpEx  + YtdDepr;
    public decimal BudEbitda    => BudGross  - BudOpEx  + BudDepr;
    public decimal FcstEbitda   => FcstGross - FcstOpEx + FcstDepr;

    public decimal YtdInterest  => SumYtd(PnlGroup.Interest);
    public decimal BudInterest  => SumBud(PnlGroup.Interest);
    public decimal FcstInterest => SumFcst(PnlGroup.Interest);

    public decimal YtdTax       => SumYtd(PnlGroup.Tax);
    public decimal BudTax       => SumBud(PnlGroup.Tax);
    public decimal FcstTax      => SumFcst(PnlGroup.Tax);

    public decimal YtdNet       => YtdEbitda  - YtdInterest  - YtdTax;
    public decimal BudNet       => BudEbitda  - BudInterest  - BudTax;
    public decimal FcstNet      => FcstEbitda - FcstInterest - FcstTax;
}

public interface IPnlService
{
    Task<PnlReport> GenerateAsync(PnlQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<PnlDrillDownLine>> GetDrillDownAsync(
        Guid tenantId, Guid accountId, bool isRevenue,
        DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<PnlBudgetReport?> GetBudgetOverlayAsync(
        PnlReport actual, Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<YearEndForecastReport?> GetYearEndForecastAsync(
        Guid tenantId, int fiscalYear, int actualThroughMonth, CancellationToken ct = default);
}

public interface IPnlExporter
{
    byte[] ToPdf(PnlReport report, PnlExportMeta meta);
    string ToCsv(PnlReport report, PnlExportMeta meta);
    byte[] ToPdf(PnlComparisonReport report, PnlExportMeta meta);
    string ToCsv(PnlComparisonReport report, PnlExportMeta meta);
    byte[] ToPdf(PnlBudgetReport report, PnlExportMeta meta);
    string ToCsv(PnlBudgetReport report, PnlExportMeta meta);
}
