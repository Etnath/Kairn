using Kairn.Application.Features.Reports;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Kairn.Infrastructure.Reports;

public class PnlExporter(IWebHostEnvironment env) : IPnlExporter
{
    // Kairn brand colours
    private static readonly string GreenHex = "#1F6040";
    private static readonly string RedHex   = "#7E2A14";
    private static readonly string StoneHex = "#8C8980";

    private readonly Lazy<byte[]> _logo = new(() =>
    {
        var path = Path.Combine(env.WebRootPath, "Logo.png");
        return File.Exists(path) ? File.ReadAllBytes(path) : [];
    });

    // ────────────────────────────────────────────────────────────────────────
    // Single-period PDF
    // ────────────────────────────────────────────────────────────────────────

    public byte[] ToPdf(PnlReport report, PnlExportMeta meta)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(ts => ts.FontSize(9).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    AddLogoHeader(col,
                        "Profit & Loss",
                        $"{report.From:dd/MM/yyyy} – {report.To:dd/MM/yyyy}");
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    void Section(string header, IEnumerable<PnlAccountLine> lines, decimal subtotal, string subtotalLabel)
                    {
                        col.Item().PaddingTop(6).Text(header).FontSize(10).Bold()
                            .FontColor(Colors.Grey.Darken2);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70);
                                c.RelativeColumn();
                                c.ConstantColumn(110);
                            });

                            foreach (var l in lines)
                            {
                                static IContainer DataCell(IContainer c) => c
                                    .BorderBottom(0.3f, Unit.Point)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).AlignMiddle();

                                table.Cell().Element(DataCell).Text(l.AccountCode).FontColor(Colors.Grey.Medium);
                                table.Cell().Element(DataCell).Text(l.AccountName);
                                table.Cell().Element(DataCell).AlignRight()
                                    .Text(l.Amount.ToString("N2"))
                                    .FontColor(l.Amount >= 0 ? Colors.Black : Colors.Red.Darken2);
                            }
                        });

                        col.Item().Padding(3).Row(row =>
                        {
                            row.RelativeItem().Text(subtotalLabel).Bold();
                            row.ConstantItem(110).AlignRight().Text(subtotal.ToString("N2")).Bold();
                        });
                    }

                    void SummaryRow(string label, decimal value)
                    {
                        var colour = value > 0 ? GreenHex : value < 0 ? RedHex : StoneHex;
                        col.Item().PaddingTop(4).Background(Colors.Grey.Lighten4).Padding(5).Row(row =>
                        {
                            row.RelativeItem().Text(label).FontSize(11).Bold().FontColor(colour);
                            row.ConstantItem(110).AlignRight().Text(value.ToString("N2")).FontSize(11).Bold().FontColor(colour);
                        });
                    }

                    var revenue  = report.Lines.Where(l => l.Group == PnlGroup.Revenue).ToList();
                    var cogs     = report.Lines.Where(l => l.Group == PnlGroup.Cogs).ToList();
                    var opex     = report.Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).ToList();
                    var interest = report.Lines.Where(l => l.Group == PnlGroup.Interest).ToList();
                    var tax      = report.Lines.Where(l => l.Group == PnlGroup.Tax).ToList();

                    if (revenue.Count > 0) Section("Revenue", revenue, report.TotalRevenue, "Total Revenue");
                    if (cogs.Count > 0)    Section("Cost of Goods Sold", cogs, report.TotalCogs, "Total COGS");

                    SummaryRow("Gross Profit", report.GrossProfit);

                    if (opex.Count > 0) Section("Operating Expenses", opex, report.TotalOperatingExpenses, "Total Operating Expenses");

                    SummaryRow("EBITDA", report.Ebitda);

                    if (interest.Count > 0) Section("Interest", interest, report.TotalInterest, "Total Interest");
                    if (tax.Count > 0)      Section("Tax", tax, report.TotalTax, "Total Tax");

                    SummaryRow("Net Income", report.NetIncome);
                });

                AddMetaFooter(page, meta);
            });
        }).GeneratePdf();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Comparison PDF
    // ────────────────────────────────────────────────────────────────────────

    public byte[] ToPdf(PnlComparisonReport report, PnlExportMeta meta)
    {
        var cur  = report.Current;
        var comp = report.Comparison;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(ts => ts.FontSize(8).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    AddLogoHeader(col,
                        "Profit & Loss — Comparison",
                        $"{cur.From:dd/MM/yyyy} – {cur.To:dd/MM/yyyy}   vs   {comp.From:dd/MM/yyyy} – {comp.To:dd/MM/yyyy}",
                        fontSize: 14);
                });

                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Item().Table(hdrTable =>
                    {
                        hdrTable.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(55); c.RelativeColumn();
                            c.ConstantColumn(85); c.ConstantColumn(85);
                            c.ConstantColumn(85); c.ConstantColumn(62);
                        });

                        static IContainer HdrCell(IContainer c) => c
                            .BorderBottom(0.8f, Unit.Point).BorderColor(Colors.Grey.Darken1)
                            .PaddingVertical(3).PaddingHorizontal(2).AlignMiddle();

                        hdrTable.Cell().Element(HdrCell).Text("Code").FontSize(7).FontColor(Colors.Grey.Medium);
                        hdrTable.Cell().Element(HdrCell).Text("Account").FontSize(7);
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text($"{cur.From:dd/MM/yy} – {cur.To:dd/MM/yy}").FontSize(7);
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text($"{comp.From:dd/MM/yy} – {comp.To:dd/MM/yy}").FontSize(7).FontColor(Colors.Grey.Medium);
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Variance").FontSize(7);
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Var %").FontSize(7);
                    });

                    void CompSection(string header, IEnumerable<PnlComparisonLine> lines,
                        decimal curSubtotal, decimal compSubtotal, string subtotalLabel)
                    {
                        col.Item().PaddingTop(6).Text(header).FontSize(10).Bold().FontColor(Colors.Grey.Darken2);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(55); c.RelativeColumn();
                                c.ConstantColumn(85); c.ConstantColumn(85);
                                c.ConstantColumn(85); c.ConstantColumn(62);
                            });

                            foreach (var l in lines)
                            {
                                static IContainer DataCell(IContainer c) => c
                                    .BorderBottom(0.3f, Unit.Point).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(2).AlignMiddle();

                                var vc  = VarColour(l.Variance, l.Group);
                                var pct = FormatVarPct(l.Variance, l.Comparison);

                                table.Cell().Element(DataCell).Text(l.AccountCode).FontColor(Colors.Grey.Medium);
                                table.Cell().Element(DataCell).Text(l.AccountName);
                                table.Cell().Element(DataCell).AlignRight().Text(l.Current.ToString("N2"));
                                table.Cell().Element(DataCell).AlignRight().Text(l.Comparison.ToString("N2")).FontColor(Colors.Grey.Medium);
                                table.Cell().Element(DataCell).AlignRight().Text(l.Variance.ToString("N2")).FontColor(vc);
                                table.Cell().Element(DataCell).AlignRight().Text(pct).FontColor(vc);
                            }
                        });

                        var subVar = curSubtotal - compSubtotal;
                        var subVc  = SummaryVarColour(subVar);
                        col.Item().Padding(2).Row(row =>
                        {
                            row.RelativeItem().Text(subtotalLabel).Bold();
                            row.ConstantItem(85).AlignRight().Text(curSubtotal.ToString("N2")).Bold();
                            row.ConstantItem(85).AlignRight().Text(compSubtotal.ToString("N2")).Bold().FontColor(Colors.Grey.Medium);
                            row.ConstantItem(85).AlignRight().Text(subVar.ToString("N2")).Bold().FontColor(subVc);
                            row.ConstantItem(62).AlignRight().Text(FormatVarPct(subVar, compSubtotal)).Bold().FontColor(subVc);
                        });
                    }

                    void CompSummary(string label, decimal curVal, decimal compVal)
                    {
                        var variance = curVal - compVal;
                        var vc  = SummaryVarColour(variance);
                        col.Item().PaddingTop(3).Background(Colors.Grey.Lighten4).Padding(4).Row(row =>
                        {
                            row.RelativeItem().Text(label).FontSize(10).Bold().FontColor(vc);
                            row.ConstantItem(85).AlignRight().Text(curVal.ToString("N2")).FontSize(10).Bold().FontColor(vc);
                            row.ConstantItem(85).AlignRight().Text(compVal.ToString("N2")).FontSize(10).Bold().FontColor(Colors.Grey.Medium);
                            row.ConstantItem(85).AlignRight().Text(variance.ToString("N2")).FontSize(10).Bold().FontColor(vc);
                            row.ConstantItem(62).AlignRight().Text(FormatVarPct(variance, compVal)).FontSize(10).Bold().FontColor(vc);
                        });
                    }

                    var revenue  = report.Lines.Where(l => l.Group == PnlGroup.Revenue).ToList();
                    var cogs     = report.Lines.Where(l => l.Group == PnlGroup.Cogs).ToList();
                    var opex     = report.Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).ToList();
                    var interest = report.Lines.Where(l => l.Group == PnlGroup.Interest).ToList();
                    var tax      = report.Lines.Where(l => l.Group == PnlGroup.Tax).ToList();

                    if (revenue.Count > 0) CompSection("Revenue", revenue, report.CurRevenue, report.CompRevenue, "Total Revenue");
                    if (cogs.Count > 0)    CompSection("Cost of Goods Sold", cogs, report.CurCogs, report.CompCogs, "Total COGS");

                    CompSummary("Gross Profit", report.CurGross, report.CompGross);

                    if (opex.Count > 0) CompSection("Operating Expenses", opex, report.CurOpEx, report.CompOpEx, "Total Operating Expenses");

                    CompSummary("EBITDA", report.CurEbitda, report.CompEbitda);

                    if (interest.Count > 0) CompSection("Interest", interest, report.CurInterest, report.CompInterest, "Total Interest");
                    if (tax.Count > 0)      CompSection("Tax", tax, report.CurTax, report.CompTax, "Total Tax");

                    CompSummary("Net Income", report.CurNet, report.CompNet);
                });

                AddMetaFooter(page, meta);
            });
        }).GeneratePdf();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Budget PDF
    // ────────────────────────────────────────────────────────────────────────

    public byte[] ToPdf(PnlBudgetReport report, PnlExportMeta meta)
    {
        var actual = report.Actual;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(ts => ts.FontSize(8).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    AddLogoHeader(col,
                        "Profit & Loss — Budget vs. Actual",
                        $"{actual.From:dd/MM/yyyy} – {actual.To:dd/MM/yyyy}",
                        fontSize: 14);
                });

                page.Content().PaddingTop(6).Column(col =>
                {
                    var bannerVar = report.ActNet - report.BudNet;
                    var bannerVc  = SummaryVarColour(bannerVar);
                    col.Item().PaddingBottom(6).Background(Colors.Grey.Lighten4).Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text("Net Income vs. Budget").FontSize(10).Bold().FontColor(bannerVc);
                        row.ConstantItem(85).AlignRight().Text(report.ActNet.ToString("N2")).FontSize(10).Bold().FontColor(bannerVc);
                        row.ConstantItem(85).AlignRight().Text(report.BudNet.ToString("N2")).FontSize(10).Bold().FontColor(Colors.Grey.Medium);
                        row.ConstantItem(85).AlignRight().Text(bannerVar.ToString("N2")).FontSize(10).Bold().FontColor(bannerVc);
                        row.ConstantItem(62).AlignRight().Text(FormatVarPct(bannerVar, report.BudNet)).FontSize(10).Bold().FontColor(bannerVc);
                    });

                    col.Item().Table(hdrTable =>
                    {
                        hdrTable.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(55); c.RelativeColumn();
                            c.ConstantColumn(85); c.ConstantColumn(85);
                            c.ConstantColumn(85); c.ConstantColumn(62);
                        });

                        static IContainer HdrCell(IContainer c) => c
                            .BorderBottom(0.8f, Unit.Point).BorderColor(Colors.Grey.Darken1)
                            .PaddingVertical(3).PaddingHorizontal(2).AlignMiddle();

                        hdrTable.Cell().Element(HdrCell).Text("Code").FontSize(7).FontColor(Colors.Grey.Medium);
                        hdrTable.Cell().Element(HdrCell).Text("Account").FontSize(7);
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Actual").FontSize(7);
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Budget").FontSize(7).FontColor(Colors.Grey.Medium);
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Variance").FontSize(7);
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Var %").FontSize(7);
                    });

                    void BudSection(string header, IEnumerable<PnlBudgetLine> lines,
                        decimal actSubtotal, decimal budSubtotal, string subtotalLabel)
                    {
                        col.Item().PaddingTop(6).Text(header).FontSize(10).Bold().FontColor(Colors.Grey.Darken2);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(55); c.RelativeColumn();
                                c.ConstantColumn(85); c.ConstantColumn(85);
                                c.ConstantColumn(85); c.ConstantColumn(62);
                            });

                            foreach (var l in lines)
                            {
                                static IContainer DataCell(IContainer c) => c
                                    .BorderBottom(0.3f, Unit.Point).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(2).AlignMiddle();

                                var vc  = VarColour(l.Variance, l.Group);
                                var pct = FormatVarPct(l.Variance, l.Budget);

                                table.Cell().Element(DataCell).Text(l.AccountCode).FontColor(Colors.Grey.Medium);
                                table.Cell().Element(DataCell).Text(l.AccountName);
                                table.Cell().Element(DataCell).AlignRight().Text(l.Actual.ToString("N2"));
                                table.Cell().Element(DataCell).AlignRight().Text(l.Budget.ToString("N2")).FontColor(Colors.Grey.Medium);
                                table.Cell().Element(DataCell).AlignRight().Text(l.Variance.ToString("N2")).FontColor(vc);
                                table.Cell().Element(DataCell).AlignRight().Text(pct).FontColor(vc);
                            }
                        });

                        var subVar = actSubtotal - budSubtotal;
                        var subVc  = SummaryVarColour(subVar);
                        col.Item().Padding(2).Row(row =>
                        {
                            row.RelativeItem().Text(subtotalLabel).Bold();
                            row.ConstantItem(85).AlignRight().Text(actSubtotal.ToString("N2")).Bold();
                            row.ConstantItem(85).AlignRight().Text(budSubtotal.ToString("N2")).Bold().FontColor(Colors.Grey.Medium);
                            row.ConstantItem(85).AlignRight().Text(subVar.ToString("N2")).Bold().FontColor(subVc);
                            row.ConstantItem(62).AlignRight().Text(FormatVarPct(subVar, budSubtotal)).Bold().FontColor(subVc);
                        });
                    }

                    void BudSummary(string label, decimal actVal, decimal budVal)
                    {
                        var variance = actVal - budVal;
                        var vc  = SummaryVarColour(variance);
                        col.Item().PaddingTop(3).Background(Colors.Grey.Lighten4).Padding(4).Row(row =>
                        {
                            row.RelativeItem().Text(label).FontSize(10).Bold().FontColor(vc);
                            row.ConstantItem(85).AlignRight().Text(actVal.ToString("N2")).FontSize(10).Bold().FontColor(vc);
                            row.ConstantItem(85).AlignRight().Text(budVal.ToString("N2")).FontSize(10).Bold().FontColor(Colors.Grey.Medium);
                            row.ConstantItem(85).AlignRight().Text(variance.ToString("N2")).FontSize(10).Bold().FontColor(vc);
                            row.ConstantItem(62).AlignRight().Text(FormatVarPct(variance, budVal)).FontSize(10).Bold().FontColor(vc);
                        });
                    }

                    var revenue  = report.Lines.Where(l => l.Group == PnlGroup.Revenue).ToList();
                    var cogs     = report.Lines.Where(l => l.Group == PnlGroup.Cogs).ToList();
                    var opex     = report.Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation).ToList();
                    var interest = report.Lines.Where(l => l.Group == PnlGroup.Interest).ToList();
                    var tax      = report.Lines.Where(l => l.Group == PnlGroup.Tax).ToList();

                    if (revenue.Count > 0) BudSection("Revenue", revenue, report.ActRevenue, report.BudRevenue, "Total Revenue");
                    if (cogs.Count > 0)    BudSection("Cost of Goods Sold", cogs, report.ActCogs, report.BudCogs, "Total COGS");

                    BudSummary("Gross Profit", report.ActGross, report.BudGross);

                    if (opex.Count > 0) BudSection("Operating Expenses", opex, report.ActOpEx, report.BudOpEx, "Total Operating Expenses");

                    BudSummary("EBITDA", report.ActEbitda, report.BudEbitda);

                    if (interest.Count > 0) BudSection("Interest", interest, report.ActInterest, report.BudInterest, "Total Interest");
                    if (tax.Count > 0)      BudSection("Tax", tax, report.ActTax, report.BudTax, "Total Tax");

                    BudSummary("Net Income", report.ActNet, report.BudNet);
                });

                AddMetaFooter(page, meta);
            });
        }).GeneratePdf();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Single-period CSV
    // ────────────────────────────────────────────────────────────────────────

    public string ToCsv(PnlReport report, PnlExportMeta meta)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\"Profit & Loss — {report.From:dd/MM/yyyy} to {report.To:dd/MM/yyyy}\"");
        sb.AppendLine($"\"Generated by: {meta.GeneratedBy} on {meta.GeneratedAt:dd/MM/yyyy HH:mm} UTC\"");
        sb.AppendLine();

        void WriteSection(string header, IEnumerable<PnlAccountLine> lines, decimal subtotal, string subtotalLabel)
        {
            sb.AppendLine($"\"{header}\"");
            sb.AppendLine("\"Code\",\"Account\",\"Amount\"");
            foreach (var l in lines)
                sb.AppendLine($"\"{l.AccountCode}\",\"{l.AccountName.Replace("\"", "\"\"")}\"," +
                              $"\"{l.Amount:N2}\"");
            sb.AppendLine($"\"{subtotalLabel}\",\"\",\"{subtotal:N2}\"");
            sb.AppendLine();
        }

        void WriteSummary(string label, decimal value)
        {
            sb.AppendLine($"\"{label}\",\"\",\"{value:N2}\"");
            sb.AppendLine();
        }

        WriteSection("Revenue", report.Lines.Where(l => l.Group == PnlGroup.Revenue), report.TotalRevenue, "Total Revenue");
        WriteSection("Cost of Goods Sold", report.Lines.Where(l => l.Group == PnlGroup.Cogs), report.TotalCogs, "Total COGS");
        WriteSummary("Gross Profit", report.GrossProfit);
        WriteSection("Operating Expenses", report.Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation), report.TotalOperatingExpenses, "Total Operating Expenses");
        WriteSummary("EBITDA", report.Ebitda);
        WriteSection("Interest", report.Lines.Where(l => l.Group == PnlGroup.Interest), report.TotalInterest, "Total Interest");
        WriteSection("Tax", report.Lines.Where(l => l.Group == PnlGroup.Tax), report.TotalTax, "Total Tax");
        WriteSummary("Net Income", report.NetIncome);

        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Comparison CSV
    // ────────────────────────────────────────────────────────────────────────

    public string ToCsv(PnlComparisonReport report, PnlExportMeta meta)
    {
        var cur  = report.Current;
        var comp = report.Comparison;
        var sb   = new StringBuilder();

        sb.AppendLine($"\"Profit & Loss — {cur.From:dd/MM/yyyy} to {cur.To:dd/MM/yyyy} vs {comp.From:dd/MM/yyyy} to {comp.To:dd/MM/yyyy}\"");
        sb.AppendLine($"\"Generated by: {meta.GeneratedBy} on {meta.GeneratedAt:dd/MM/yyyy HH:mm} UTC\"");
        sb.AppendLine();
        sb.AppendLine($"\"Code\",\"Account\",\"Current ({cur.From:dd/MM/yy}–{cur.To:dd/MM/yy})\",\"Comparison ({comp.From:dd/MM/yy}–{comp.To:dd/MM/yy})\",\"Variance\",\"Var %\"");
        sb.AppendLine();

        void WriteSection(string header, IEnumerable<PnlComparisonLine> lines,
            decimal curSub, decimal compSub, string subtotalLabel)
        {
            sb.AppendLine($"\"{header}\"");
            foreach (var l in lines)
                sb.AppendLine($"\"{l.AccountCode}\"," +
                              $"\"{l.AccountName.Replace("\"", "\"\"")}\"," +
                              $"\"{l.Current:N2}\",\"{l.Comparison:N2}\"," +
                              $"\"{l.Variance:N2}\",\"{FormatVarPct(l.Variance, l.Comparison)}\"");

            var subVar = curSub - compSub;
            sb.AppendLine($"\"{subtotalLabel}\",\"\",\"{curSub:N2}\",\"{compSub:N2}\"," +
                          $"\"{subVar:N2}\",\"{FormatVarPct(subVar, compSub)}\"");
            sb.AppendLine();
        }

        void WriteSummary(string label, decimal curVal, decimal compVal)
        {
            var variance = curVal - compVal;
            sb.AppendLine($"\"{label}\",\"\",\"{curVal:N2}\",\"{compVal:N2}\"," +
                          $"\"{variance:N2}\",\"{FormatVarPct(variance, compVal)}\"");
            sb.AppendLine();
        }

        WriteSection("Revenue", report.Lines.Where(l => l.Group == PnlGroup.Revenue), report.CurRevenue, report.CompRevenue, "Total Revenue");
        WriteSection("Cost of Goods Sold", report.Lines.Where(l => l.Group == PnlGroup.Cogs), report.CurCogs, report.CompCogs, "Total COGS");
        WriteSummary("Gross Profit", report.CurGross, report.CompGross);
        WriteSection("Operating Expenses", report.Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation), report.CurOpEx, report.CompOpEx, "Total Operating Expenses");
        WriteSummary("EBITDA", report.CurEbitda, report.CompEbitda);
        WriteSection("Interest", report.Lines.Where(l => l.Group == PnlGroup.Interest), report.CurInterest, report.CompInterest, "Total Interest");
        WriteSection("Tax", report.Lines.Where(l => l.Group == PnlGroup.Tax), report.CurTax, report.CompTax, "Total Tax");
        WriteSummary("Net Income", report.CurNet, report.CompNet);

        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Budget CSV
    // ────────────────────────────────────────────────────────────────────────

    public string ToCsv(PnlBudgetReport report, PnlExportMeta meta)
    {
        var actual = report.Actual;
        var sb     = new StringBuilder();

        sb.AppendLine($"\"Profit & Loss — Budget vs. Actual — {actual.From:dd/MM/yyyy} to {actual.To:dd/MM/yyyy}\"");
        sb.AppendLine($"\"Generated by: {meta.GeneratedBy} on {meta.GeneratedAt:dd/MM/yyyy HH:mm} UTC\"");
        sb.AppendLine();
        sb.AppendLine($"\"Code\",\"Account\",\"Actual\",\"Budget\",\"Variance\",\"Var %\"");
        sb.AppendLine();

        void WriteSection(string header, IEnumerable<PnlBudgetLine> lines,
            decimal actSub, decimal budSub, string subtotalLabel)
        {
            sb.AppendLine($"\"{header}\"");
            foreach (var l in lines)
                sb.AppendLine($"\"{l.AccountCode}\"," +
                              $"\"{l.AccountName.Replace("\"", "\"\"")}\"," +
                              $"\"{l.Actual:N2}\",\"{l.Budget:N2}\"," +
                              $"\"{l.Variance:N2}\",\"{FormatVarPct(l.Variance, l.Budget)}\"");

            var subVar = actSub - budSub;
            sb.AppendLine($"\"{subtotalLabel}\",\"\",\"{actSub:N2}\",\"{budSub:N2}\"," +
                          $"\"{subVar:N2}\",\"{FormatVarPct(subVar, budSub)}\"");
            sb.AppendLine();
        }

        void WriteSummary(string label, decimal actVal, decimal budVal)
        {
            var variance = actVal - budVal;
            sb.AppendLine($"\"{label}\",\"\",\"{actVal:N2}\",\"{budVal:N2}\"," +
                          $"\"{variance:N2}\",\"{FormatVarPct(variance, budVal)}\"");
            sb.AppendLine();
        }

        WriteSection("Revenue", report.Lines.Where(l => l.Group == PnlGroup.Revenue), report.ActRevenue, report.BudRevenue, "Total Revenue");
        WriteSection("Cost of Goods Sold", report.Lines.Where(l => l.Group == PnlGroup.Cogs), report.ActCogs, report.BudCogs, "Total COGS");
        WriteSummary("Gross Profit", report.ActGross, report.BudGross);
        WriteSection("Operating Expenses", report.Lines.Where(l => l.Group is PnlGroup.OperatingExpenses or PnlGroup.Depreciation), report.ActOpEx, report.BudOpEx, "Total Operating Expenses");
        WriteSummary("EBITDA", report.ActEbitda, report.BudEbitda);
        WriteSection("Interest", report.Lines.Where(l => l.Group == PnlGroup.Interest), report.ActInterest, report.BudInterest, "Total Interest");
        WriteSection("Tax", report.Lines.Where(l => l.Group == PnlGroup.Tax), report.ActTax, report.BudTax, "Total Tax");
        WriteSummary("Net Income", report.ActNet, report.BudNet);

        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ────────────────────────────────────────────────────────────────────────

    private void AddLogoHeader(ColumnDescriptor col, string title, string subtitle, int fontSize = 16)
    {
        var logo = _logo.Value;
        col.Item().Row(row =>
        {
            if (logo.Length > 0)
                row.ConstantItem(50).Image(logo);
            row.RelativeItem()
               .PaddingLeft(logo.Length > 0 ? 10 : 0)
               .Column(inner =>
               {
                   inner.Item().Text(title).FontSize(fontSize).Bold();
                   inner.Item().Text(subtitle).FontSize(9).FontColor(Colors.Grey.Medium);
               });
        });
        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
    }

    private static void AddMetaFooter(PageDescriptor page, PnlExportMeta meta)
    {
        page.Footer().Row(row =>
        {
            row.RelativeItem()
               .Text($"Generated by {meta.GeneratedBy} · {meta.GeneratedAt:dd/MM/yyyy HH:mm} UTC")
               .FontSize(7).FontColor(Colors.Grey.Medium);
            row.ConstantItem(60).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(ts => ts.FontSize(7).FontColor(Colors.Grey.Medium));
                text.Span("Page "); text.CurrentPageNumber(); text.Span(" / "); text.TotalPages();
            });
        });
    }

    private static string VarColour(decimal variance, PnlGroup group)
    {
        if (variance == 0m) return StoneHex;
        bool favorable = group == PnlGroup.Revenue ? variance > 0 : variance < 0;
        return favorable ? GreenHex : RedHex;
    }

    private static string SummaryVarColour(decimal variance) =>
        variance > 0 ? GreenHex : variance < 0 ? RedHex : StoneHex;

    private static string FormatVarPct(decimal variance, decimal comparison)
    {
        if (comparison == 0m) return "N/A";
        var pct = Math.Round((variance / Math.Abs(comparison)) * 100m, 1);
        return $"{pct:+0.0;-0.0;0.0}%";
    }
}
