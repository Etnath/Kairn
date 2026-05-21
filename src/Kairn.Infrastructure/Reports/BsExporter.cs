using Kairn.Application.Features.Reports;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Kairn.Infrastructure.Reports;

public class BsExporter(IWebHostEnvironment env) : IBsExporter
{
    private static readonly string GreenHex = "#1F6040";
    private static readonly string RedHex   = "#7E2A14";

    private readonly Lazy<byte[]> _logo = new(() =>
    {
        var path = Path.Combine(env.WebRootPath, "Logo.png");
        return File.Exists(path) ? File.ReadAllBytes(path) : [];
    });

    private sealed record MergedLine(string Code, string Name, bool IsCurrent, decimal Primary, decimal Comp)
    {
        public decimal Movement => Primary - Comp;
    }

    private static List<MergedLine> MergeLines(
        IReadOnlyList<BsAccountLine> primary, IReadOnlyList<BsAccountLine> comparison)
    {
        var compDict = comparison.ToDictionary(l => l.AccountId);
        var primIds  = primary.Select(l => l.AccountId).ToHashSet();
        var result   = primary.Select(l =>
            new MergedLine(l.Code, l.Name, l.IsCurrent, l.Balance,
                compDict.GetValueOrDefault(l.AccountId)?.Balance ?? 0m)).ToList();
        foreach (var cl in comparison.Where(l => !primIds.Contains(l.AccountId)))
            result.Add(new MergedLine(cl.Code, cl.Name, cl.IsCurrent, 0m, cl.Balance));
        return result.OrderBy(l => l.Code).ToList();
    }

    private static string MovColour(decimal movement, bool isAsset) =>
        isAsset
            ? (movement > 0 ? GreenHex : movement < 0 ? RedHex : (string)Colors.Black)
            : (string)Colors.Black;

    public byte[] ToPdf(BsReport report, BsExportMeta meta)
    {
        var hasComp = report.HasComparison;
        var comp    = report.Comparison;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(ts => ts.FontSize(9).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    var subtitle = hasComp
                        ? $"As of {report.AsOf:dd/MM/yyyy}  vs  {comp!.AsOf:dd/MM/yyyy}"
                        : $"As of {report.AsOf:dd/MM/yyyy}";
                    AddLogoHeader(col, "Balance Sheet", subtitle);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    static IContainer DataCell(IContainer c) => c
                        .BorderBottom(0.3f, Unit.Point)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(3).AlignMiddle();

                    // ── Single-column helpers ──────────────────────────────────
                    void AccountSection(string header, IEnumerable<BsAccountLine> lines,
                        decimal subtotal, string subtotalLabel)
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
                                table.Cell().Element(DataCell).Text(l.Code).FontColor(Colors.Grey.Medium);
                                table.Cell().Element(DataCell).Text(l.Name);
                                table.Cell().Element(DataCell).AlignRight()
                                    .Text(l.Balance.ToString("N2"))
                                    .FontColor(l.Balance >= 0 ? Colors.Black : Colors.Red.Darken2);
                            }
                        });

                        col.Item().Padding(3).Row(row =>
                        {
                            row.RelativeItem().Text(subtotalLabel).Bold();
                            row.ConstantItem(110).AlignRight().Text(subtotal.ToString("N2")).Bold();
                        });
                    }

                    void TotalRow(string label, decimal value, bool useSignColour = false)
                    {
                        var colour = useSignColour
                            ? (value >= 0 ? GreenHex : RedHex)
                            : (string)Colors.Black;
                        col.Item().PaddingTop(4).Background(Colors.Grey.Lighten4).Padding(5).Row(row =>
                        {
                            row.RelativeItem().Text(label).FontSize(11).Bold().FontColor(colour);
                            row.ConstantItem(110).AlignRight().Text(value.ToString("N2"))
                                .FontSize(11).Bold().FontColor(colour);
                        });
                    }

                    // ── Three-column comparison helpers ────────────────────────
                    void CompareAccountSection(string header,
                        IEnumerable<BsAccountLine> lines, IEnumerable<BsAccountLine> compLines,
                        decimal subtotal, decimal compSubtotal, string subtotalLabel,
                        bool isAsset = false)
                    {
                        col.Item().PaddingTop(6).Text(header).FontSize(10).Bold()
                            .FontColor(Colors.Grey.Darken2);

                        var merged = MergeLines(lines.ToList(), compLines.ToList());

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70);
                                c.RelativeColumn();
                                c.ConstantColumn(90);
                                c.ConstantColumn(90);
                                c.ConstantColumn(90);
                            });

                            foreach (var l in merged)
                            {
                                table.Cell().Element(DataCell).Text(l.Code).FontColor(Colors.Grey.Medium);
                                table.Cell().Element(DataCell).Text(l.Name);
                                table.Cell().Element(DataCell).AlignRight().Text(l.Primary.ToString("N2"));
                                table.Cell().Element(DataCell).AlignRight()
                                    .Text(l.Comp.ToString("N2")).FontColor(Colors.Grey.Medium);
                                table.Cell().Element(DataCell).AlignRight()
                                    .Text(l.Movement.ToString("N2"))
                                    .FontColor(MovColour(l.Movement, isAsset));
                            }
                        });

                        var movSub = subtotal - compSubtotal;
                        col.Item().Padding(3).Row(row =>
                        {
                            row.RelativeItem().Text(subtotalLabel).Bold();
                            row.ConstantItem(90).AlignRight().Text(subtotal.ToString("N2")).Bold();
                            row.ConstantItem(90).AlignRight().Text(compSubtotal.ToString("N2"))
                                .Bold().FontColor(Colors.Grey.Medium);
                            row.ConstantItem(90).AlignRight().Text(movSub.ToString("N2"))
                                .Bold().FontColor(MovColour(movSub, isAsset));
                        });
                    }

                    void CompareTotalRow(string label, decimal primary, decimal comparison,
                        bool useSignColour = false, bool isAsset = false)
                    {
                        var colour  = useSignColour ? (primary >= 0 ? GreenHex : RedHex) : (string)Colors.Black;
                        var movement = primary - comparison;
                        var movCol  = MovColour(movement, isAsset);

                        col.Item().PaddingTop(4).Background(Colors.Grey.Lighten4).Padding(5).Row(row =>
                        {
                            row.RelativeItem().Text(label).FontSize(11).Bold().FontColor(colour);
                            row.ConstantItem(90).AlignRight().Text(primary.ToString("N2"))
                                .FontSize(11).Bold().FontColor(colour);
                            row.ConstantItem(90).AlignRight().Text(comparison.ToString("N2"))
                                .FontSize(11).Bold().FontColor(Colors.Grey.Medium);
                            row.ConstantItem(90).AlignRight().Text(movement.ToString("N2"))
                                .FontSize(11).Bold().FontColor(movCol);
                        });
                    }

                    // ── Render ─────────────────────────────────────────────────
                    if (hasComp)
                    {
                        // Date column headers
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.ConstantItem(70);
                            row.RelativeItem();
                            row.ConstantItem(90).AlignRight()
                                .Text(report.AsOf.ToString("dd/MM/yyyy")).FontSize(8).Bold();
                            row.ConstantItem(90).AlignRight()
                                .Text(comp!.AsOf.ToString("dd/MM/yyyy")).FontSize(8).FontColor(Colors.Grey.Medium);
                            row.ConstantItem(90).AlignRight()
                                .Text("Movement").FontSize(8).Bold();
                        });
                        col.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        // Assets
                        col.Item().PaddingTop(8).Text("ASSETS").FontSize(12).Bold();
                        col.Item().LineHorizontal(0.8f).LineColor(Colors.Grey.Darken1);

                        var curA    = report.AssetLines.Where(l => l.IsCurrent).ToList();
                        var nonA    = report.AssetLines.Where(l => !l.IsCurrent).ToList();
                        var cCurA   = comp!.AssetLines.Where(l => l.IsCurrent).ToList();
                        var cNonA   = comp.AssetLines.Where(l => !l.IsCurrent).ToList();

                        if (curA.Count > 0 || cCurA.Count > 0)
                            CompareAccountSection("Current Assets", curA, cCurA,
                                report.TotalCurrentAssets, comp.TotalCurrentAssets,
                                "Total Current Assets", isAsset: true);
                        if (nonA.Count > 0 || cNonA.Count > 0)
                            CompareAccountSection("Non-Current Assets", nonA, cNonA,
                                report.TotalNonCurrentAssets, comp.TotalNonCurrentAssets,
                                "Total Non-Current Assets", isAsset: true);

                        CompareTotalRow("Total Assets", report.TotalAssets, comp.TotalAssets,
                            useSignColour: true, isAsset: true);

                        // Liabilities
                        col.Item().PaddingTop(10).Text("LIABILITIES").FontSize(12).Bold();
                        col.Item().LineHorizontal(0.8f).LineColor(Colors.Grey.Darken1);

                        var curL  = report.LiabilityLines.Where(l => l.IsCurrent).ToList();
                        var nonL  = report.LiabilityLines.Where(l => !l.IsCurrent).ToList();
                        var cCurL = comp.LiabilityLines.Where(l => l.IsCurrent).ToList();
                        var cNonL = comp.LiabilityLines.Where(l => !l.IsCurrent).ToList();

                        if (curL.Count > 0 || cCurL.Count > 0)
                            CompareAccountSection("Current Liabilities", curL, cCurL,
                                report.TotalCurrentLiabilities, comp.TotalCurrentLiabilities,
                                "Total Current Liabilities");
                        if (nonL.Count > 0 || cNonL.Count > 0)
                            CompareAccountSection("Non-Current Liabilities", nonL, cNonL,
                                report.TotalNonCurrentLiabilities, comp.TotalNonCurrentLiabilities,
                                "Total Non-Current Liabilities");

                        CompareTotalRow("Total Liabilities", report.TotalLiabilities, comp.TotalLiabilities);

                        // Equity
                        col.Item().PaddingTop(10).Text("EQUITY").FontSize(12).Bold();
                        col.Item().LineHorizontal(0.8f).LineColor(Colors.Grey.Darken1);

                        if (report.EquityLines.Count > 0 || comp.EquityLines.Count > 0)
                            CompareAccountSection("Share Capital", report.EquityLines, comp.EquityLines,
                                report.TotalShareCapital, comp.TotalShareCapital, "Total Share Capital");

                        // Retained Earnings + Current Year Earnings rows
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70); c.RelativeColumn();
                                c.ConstantColumn(90); c.ConstantColumn(90); c.ConstantColumn(90);
                            });

                            var reMovement = report.RetainedEarnings - comp.RetainedEarnings;
                            table.Cell().Element(DataCell).Text(string.Empty);
                            table.Cell().Element(DataCell).Text("Retained Earnings");
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(report.RetainedEarnings.ToString("N2"));
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(comp.RetainedEarnings.ToString("N2")).FontColor(Colors.Grey.Medium);
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(reMovement.ToString("N2")).FontColor(MovColour(reMovement, false));

                            var cyeMovement = report.CurrentYearEarnings - comp.CurrentYearEarnings;
                            table.Cell().Element(DataCell).Text(string.Empty);
                            table.Cell().Element(DataCell).Text("Current Year Earnings");
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(report.CurrentYearEarnings.ToString("N2"));
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(comp.CurrentYearEarnings.ToString("N2")).FontColor(Colors.Grey.Medium);
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(cyeMovement.ToString("N2")).FontColor(MovColour(cyeMovement, false));
                        });

                        CompareTotalRow("Total Equity", report.TotalEquity, comp.TotalEquity,
                            useSignColour: true);

                        // Balance checks (both dates)
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                        void BalanceCheckRow(BsReport r, string label)
                        {
                            var liabEq = r.TotalLiabilities + r.TotalEquity;
                            var colour = r.IsBalanced ? GreenHex : RedHex;
                            var text   = r.IsBalanced
                                ? $"{label} — ✓ Assets ({r.TotalAssets:N2}) = Liabilities + Equity ({liabEq:N2})"
                                : $"{label} — ⚠ Balance failed: Assets ({r.TotalAssets:N2}) ≠ Liabilities + Equity ({liabEq:N2})";
                            col.Item().PaddingTop(3).Text(text).FontSize(9).Bold().FontColor(colour);
                        }

                        BalanceCheckRow(report, report.AsOf.ToString("dd/MM/yyyy"));
                        BalanceCheckRow(comp,   comp.AsOf.ToString("dd/MM/yyyy"));
                    }
                    else
                    {
                        // ── Assets ──
                        col.Item().PaddingTop(2).Text("ASSETS").FontSize(12).Bold();
                        col.Item().LineHorizontal(0.8f).LineColor(Colors.Grey.Darken1);

                        var currentAssets    = report.AssetLines.Where(l => l.IsCurrent).ToList();
                        var nonCurrentAssets = report.AssetLines.Where(l => !l.IsCurrent).ToList();

                        if (currentAssets.Count > 0)
                            AccountSection("Current Assets", currentAssets,
                                report.TotalCurrentAssets, "Total Current Assets");
                        if (nonCurrentAssets.Count > 0)
                            AccountSection("Non-Current Assets", nonCurrentAssets,
                                report.TotalNonCurrentAssets, "Total Non-Current Assets");

                        TotalRow("Total Assets", report.TotalAssets, useSignColour: true);

                        // ── Liabilities ──
                        col.Item().PaddingTop(10).Text("LIABILITIES").FontSize(12).Bold();
                        col.Item().LineHorizontal(0.8f).LineColor(Colors.Grey.Darken1);

                        var currentLiab    = report.LiabilityLines.Where(l => l.IsCurrent).ToList();
                        var nonCurrentLiab = report.LiabilityLines.Where(l => !l.IsCurrent).ToList();

                        if (currentLiab.Count > 0)
                            AccountSection("Current Liabilities", currentLiab,
                                report.TotalCurrentLiabilities, "Total Current Liabilities");
                        if (nonCurrentLiab.Count > 0)
                            AccountSection("Non-Current Liabilities", nonCurrentLiab,
                                report.TotalNonCurrentLiabilities, "Total Non-Current Liabilities");

                        TotalRow("Total Liabilities", report.TotalLiabilities);

                        // ── Equity ──
                        col.Item().PaddingTop(10).Text("EQUITY").FontSize(12).Bold();
                        col.Item().LineHorizontal(0.8f).LineColor(Colors.Grey.Darken1);

                        if (report.EquityLines.Count > 0)
                            AccountSection("Share Capital", report.EquityLines,
                                report.TotalShareCapital, "Total Share Capital");

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70); c.RelativeColumn(); c.ConstantColumn(110);
                            });

                            table.Cell().Element(DataCell).Text(string.Empty);
                            table.Cell().Element(DataCell).Text("Retained Earnings");
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(report.RetainedEarnings.ToString("N2"))
                                .FontColor(report.RetainedEarnings >= 0 ? Colors.Black : Colors.Red.Darken2);

                            table.Cell().Element(DataCell).Text(string.Empty);
                            table.Cell().Element(DataCell).Text("Current Year Earnings");
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(report.CurrentYearEarnings.ToString("N2"))
                                .FontColor(report.CurrentYearEarnings >= 0 ? Colors.Black : Colors.Red.Darken2);
                        });

                        TotalRow("Total Equity", report.TotalEquity, useSignColour: true);

                        // ── Balance check ──
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                        var liabPlusEquity = report.TotalLiabilities + report.TotalEquity;
                        if (report.IsBalanced)
                        {
                            col.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"Total Liabilities + Equity: {liabPlusEquity:N2}  =  Total Assets: {report.TotalAssets:N2}")
                                    .FontSize(9).Bold().FontColor(GreenHex);
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"⚠ Balance check failed: Assets ({report.TotalAssets:N2}) ≠ Liabilities + Equity ({liabPlusEquity:N2})")
                                    .FontSize(9).Bold().FontColor(RedHex);
                            });
                        }
                    }
                });

                AddMetaFooter(page, meta);
            });
        }).GeneratePdf();
    }

    public string ToCsv(BsReport report, BsExportMeta meta)
    {
        var sb = new StringBuilder();
        var hasComp = report.HasComparison;
        var comp    = report.Comparison;

        var title = hasComp
            ? $"Balance Sheet — As of {report.AsOf:dd/MM/yyyy} vs {comp!.AsOf:dd/MM/yyyy}"
            : $"Balance Sheet — As of {report.AsOf:dd/MM/yyyy}";

        sb.AppendLine($"\"{title}\"");
        sb.AppendLine($"\"Generated by: {meta.GeneratedBy} on {meta.GeneratedAt:dd/MM/yyyy HH:mm} UTC\"");
        sb.AppendLine();

        if (hasComp)
        {
            void WriteCompareSection(string header,
                IReadOnlyList<BsAccountLine> lines, IReadOnlyList<BsAccountLine> compLines,
                decimal subtotal, decimal compSubtotal, string subtotalLabel)
            {
                sb.AppendLine($"\"{header}\"");
                sb.AppendLine($"\"Code\",\"Account\",\"{report.AsOf:dd/MM/yyyy}\",\"{comp!.AsOf:dd/MM/yyyy}\",\"Movement\"");
                var merged = MergeLines(lines, compLines);
                foreach (var l in merged)
                    sb.AppendLine($"\"{l.Code}\",\"{l.Name.Replace("\"", "\"\"")}\",\"{l.Primary:N2}\",\"{l.Comp:N2}\",\"{l.Movement:N2}\"");
                var movSub = subtotal - compSubtotal;
                sb.AppendLine($"\"{subtotalLabel}\",\"\",\"{subtotal:N2}\",\"{compSubtotal:N2}\",\"{movSub:N2}\"");
                sb.AppendLine();
            }

            sb.AppendLine("\"ASSETS\"");
            sb.AppendLine();
            WriteCompareSection("Current Assets",
                report.AssetLines.Where(l => l.IsCurrent).ToList(),
                comp!.AssetLines.Where(l => l.IsCurrent).ToList(),
                report.TotalCurrentAssets, comp.TotalCurrentAssets, "Total Current Assets");
            WriteCompareSection("Non-Current Assets",
                report.AssetLines.Where(l => !l.IsCurrent).ToList(),
                comp.AssetLines.Where(l => !l.IsCurrent).ToList(),
                report.TotalNonCurrentAssets, comp.TotalNonCurrentAssets, "Total Non-Current Assets");
            var aMov = report.TotalAssets - comp.TotalAssets;
            sb.AppendLine($"\"Total Assets\",\"\",\"{report.TotalAssets:N2}\",\"{comp.TotalAssets:N2}\",\"{aMov:N2}\"");
            sb.AppendLine();

            sb.AppendLine("\"LIABILITIES\"");
            sb.AppendLine();
            WriteCompareSection("Current Liabilities",
                report.LiabilityLines.Where(l => l.IsCurrent).ToList(),
                comp.LiabilityLines.Where(l => l.IsCurrent).ToList(),
                report.TotalCurrentLiabilities, comp.TotalCurrentLiabilities, "Total Current Liabilities");
            WriteCompareSection("Non-Current Liabilities",
                report.LiabilityLines.Where(l => !l.IsCurrent).ToList(),
                comp.LiabilityLines.Where(l => !l.IsCurrent).ToList(),
                report.TotalNonCurrentLiabilities, comp.TotalNonCurrentLiabilities, "Total Non-Current Liabilities");
            var lMov = report.TotalLiabilities - comp.TotalLiabilities;
            sb.AppendLine($"\"Total Liabilities\",\"\",\"{report.TotalLiabilities:N2}\",\"{comp.TotalLiabilities:N2}\",\"{lMov:N2}\"");
            sb.AppendLine();

            sb.AppendLine("\"EQUITY\"");
            sb.AppendLine();
            WriteCompareSection("Share Capital", report.EquityLines, comp.EquityLines,
                report.TotalShareCapital, comp.TotalShareCapital, "Total Share Capital");
            var reMov  = report.RetainedEarnings    - comp.RetainedEarnings;
            var cyeMov = report.CurrentYearEarnings - comp.CurrentYearEarnings;
            var eqMov  = report.TotalEquity         - comp.TotalEquity;
            sb.AppendLine($"\"\",\"Retained Earnings\",\"{report.RetainedEarnings:N2}\",\"{comp.RetainedEarnings:N2}\",\"{reMov:N2}\"");
            sb.AppendLine($"\"\",\"Current Year Earnings\",\"{report.CurrentYearEarnings:N2}\",\"{comp.CurrentYearEarnings:N2}\",\"{cyeMov:N2}\"");
            sb.AppendLine($"\"Total Equity\",\"\",\"{report.TotalEquity:N2}\",\"{comp.TotalEquity:N2}\",\"{eqMov:N2}\"");
            sb.AppendLine();

            var liabEqP = report.TotalLiabilities + report.TotalEquity;
            var liabEqC = comp.TotalLiabilities   + comp.TotalEquity;
            sb.AppendLine(report.IsBalanced
                ? $"\"Primary balance OK — Assets ({report.TotalAssets:N2}) = Liabilities + Equity ({liabEqP:N2})\""
                : $"\"WARNING: Primary balance failed — Assets ({report.TotalAssets:N2}) ≠ Liabilities + Equity ({liabEqP:N2})\"");
            sb.AppendLine(comp.IsBalanced
                ? $"\"Comparison balance OK — Assets ({comp.TotalAssets:N2}) = Liabilities + Equity ({liabEqC:N2})\""
                : $"\"WARNING: Comparison balance failed — Assets ({comp.TotalAssets:N2}) ≠ Liabilities + Equity ({liabEqC:N2})\"");
        }
        else
        {
            void WriteSection(string header, IEnumerable<BsAccountLine> lines, decimal subtotal, string subtotalLabel)
            {
                sb.AppendLine($"\"{header}\"");
                sb.AppendLine("\"Code\",\"Account\",\"Balance\"");
                foreach (var l in lines)
                    sb.AppendLine($"\"{l.Code}\",\"{l.Name.Replace("\"", "\"\"")}\",\"{l.Balance:N2}\"");
                sb.AppendLine($"\"{subtotalLabel}\",\"\",\"{subtotal:N2}\"");
                sb.AppendLine();
            }

            sb.AppendLine("\"ASSETS\"");
            sb.AppendLine();
            WriteSection("Current Assets", report.AssetLines.Where(l => l.IsCurrent),
                report.TotalCurrentAssets, "Total Current Assets");
            WriteSection("Non-Current Assets", report.AssetLines.Where(l => !l.IsCurrent),
                report.TotalNonCurrentAssets, "Total Non-Current Assets");
            sb.AppendLine($"\"Total Assets\",\"\",\"{report.TotalAssets:N2}\"");
            sb.AppendLine();

            sb.AppendLine("\"LIABILITIES\"");
            sb.AppendLine();
            WriteSection("Current Liabilities", report.LiabilityLines.Where(l => l.IsCurrent),
                report.TotalCurrentLiabilities, "Total Current Liabilities");
            WriteSection("Non-Current Liabilities", report.LiabilityLines.Where(l => !l.IsCurrent),
                report.TotalNonCurrentLiabilities, "Total Non-Current Liabilities");
            sb.AppendLine($"\"Total Liabilities\",\"\",\"{report.TotalLiabilities:N2}\"");
            sb.AppendLine();

            sb.AppendLine("\"EQUITY\"");
            sb.AppendLine();
            WriteSection("Share Capital", report.EquityLines, report.TotalShareCapital, "Total Share Capital");
            sb.AppendLine($"\"\",\"Retained Earnings\",\"{report.RetainedEarnings:N2}\"");
            sb.AppendLine($"\"\",\"Current Year Earnings\",\"{report.CurrentYearEarnings:N2}\"");
            sb.AppendLine($"\"Total Equity\",\"\",\"{report.TotalEquity:N2}\"");
            sb.AppendLine();

            var liabPlusEquity = report.TotalLiabilities + report.TotalEquity;
            sb.AppendLine(report.IsBalanced
                ? $"\"Balance check: OK — Assets ({report.TotalAssets:N2}) = Liabilities + Equity ({liabPlusEquity:N2})\""
                : $"\"WARNING: Balance check failed — Assets ({report.TotalAssets:N2}) ≠ Liabilities + Equity ({liabPlusEquity:N2})\"");
        }

        return sb.ToString();
    }

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

    private static void AddMetaFooter(PageDescriptor page, BsExportMeta meta)
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
}
