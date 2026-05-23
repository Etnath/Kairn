using Kairn.Application.Features.MarginAnalysis;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Kairn.Infrastructure.Reports;

public class GrossMarginExporter(IWebHostEnvironment env) : IGrossMarginExporter
{
    private static readonly string SignalHex = "#7E2A14";
    private static readonly string GreenHex  = "#1F6040";
    private static readonly string StoneHex  = "#8C8980";

    private readonly Lazy<byte[]> _logo = new(() =>
    {
        var path = Path.Combine(env.WebRootPath, "Logo.png");
        return File.Exists(path) ? File.ReadAllBytes(path) : [];
    });

    public byte[] ToPdf(GrossMarginReport report, GrossMarginExportMeta meta)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(ts => ts.FontSize(9).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    var logo = _logo.Value;
                    col.Item().Row(row =>
                    {
                        if (logo.Length > 0) row.ConstantItem(50).Image(logo);
                        row.RelativeItem().PaddingLeft(logo.Length > 0 ? 10 : 0).Column(inner =>
                        {
                            inner.Item().Text("Gross Margin Analysis").FontSize(16).Bold();
                            inner.Item().Text($"{report.From:dd/MM/yyyy} – {report.To:dd/MM/yyyy}")
                                 .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    // Column header row
                    col.Item().Table(hdrTable =>
                    {
                        hdrTable.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(100); c.ConstantColumn(100);
                            c.ConstantColumn(100); c.ConstantColumn(75);
                        });

                        static IContainer HdrCell(IContainer c) => c
                            .BorderBottom(0.8f, Unit.Point).BorderColor(Colors.Grey.Darken1)
                            .PaddingVertical(4).PaddingHorizontal(4).AlignMiddle();

                        hdrTable.Cell().Element(HdrCell).Text("Product / Service Line").Bold();
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Revenue").Bold();
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("COGS").Bold();
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Gross Profit").Bold();
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Margin %").Bold();
                    });

                    // Data rows
                    col.Item().Table(dataTable =>
                    {
                        dataTable.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(100); c.ConstantColumn(100);
                            c.ConstantColumn(100); c.ConstantColumn(75);
                        });

                        foreach (var row in report.Rows)
                        {
                            var bg      = row.BelowThreshold ? "#FAECE8" : "#FFFFFF";
                            var textCol = row.BelowThreshold ? SignalHex  : "#000000";
                            var pctStr  = row.GrossMarginPct.HasValue
                                ? $"{row.GrossMarginPct.Value:0.0}%"
                                : "N/A";

                            IContainer DataCell(IContainer c) => c
                                .BorderBottom(0.3f, Unit.Point).BorderColor(Colors.Grey.Lighten2)
                                .Background(bg).Padding(4).AlignMiddle();

                            dataTable.Cell().Element(DataCell).Text(row.Name).FontColor(textCol);
                            dataTable.Cell().Element(DataCell).AlignRight()
                                .Text(row.Revenue.ToString("N2")).FontColor(textCol);
                            dataTable.Cell().Element(DataCell).AlignRight()
                                .Text(row.Cogs.ToString("N2")).FontColor(textCol);
                            dataTable.Cell().Element(DataCell).AlignRight()
                                .Text(row.GrossProfit.ToString("N2")).FontColor(textCol);
                            dataTable.Cell().Element(DataCell).AlignRight()
                                .Text(pctStr).FontColor(textCol);
                        }
                    });

                    // Totals row
                    var totalPct = report.TotalMarginPct.HasValue
                        ? $"{report.TotalMarginPct.Value:0.0}%"
                        : "N/A";
                    var totColour = report.TotalGrossProfit > 0 ? GreenHex
                        : report.TotalGrossProfit < 0 ? SignalHex
                        : StoneHex;

                    col.Item().BorderTop(1, Unit.Point).BorderColor(Colors.Grey.Darken1)
                       .Background(Colors.Grey.Lighten4).Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text("Total").Bold().FontSize(10).FontColor(totColour);
                        row.ConstantItem(100).AlignRight()
                            .Text(report.TotalRevenue.ToString("N2")).Bold().FontSize(10).FontColor(totColour);
                        row.ConstantItem(100).AlignRight()
                            .Text(report.TotalCogs.ToString("N2")).Bold().FontSize(10).FontColor(totColour);
                        row.ConstantItem(100).AlignRight()
                            .Text(report.TotalGrossProfit.ToString("N2")).Bold().FontSize(10).FontColor(totColour);
                        row.ConstantItem(75).AlignRight()
                            .Text(totalPct).Bold().FontSize(10).FontColor(totColour);
                    });
                });

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
            });
        }).GeneratePdf();
    }

    public string ToCsv(GrossMarginReport report, GrossMarginExportMeta meta)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\"Gross Margin Analysis — {report.From:dd/MM/yyyy} to {report.To:dd/MM/yyyy}\"");
        sb.AppendLine($"\"Generated by: {meta.GeneratedBy} on {meta.GeneratedAt:dd/MM/yyyy HH:mm} UTC\"");
        sb.AppendLine();
        sb.AppendLine("\"Product / Service Line\",\"Revenue\",\"COGS\",\"Gross Profit\",\"Gross Margin %\"");

        foreach (var row in report.Rows)
        {
            var pct = row.GrossMarginPct.HasValue ? $"{row.GrossMarginPct.Value:0.0}%" : "N/A";
            sb.AppendLine($"\"{row.Name.Replace("\"", "\"\"")}\"," +
                          $"\"{row.Revenue:N2}\",\"{row.Cogs:N2}\"," +
                          $"\"{row.GrossProfit:N2}\",\"{pct}\"");
        }

        sb.AppendLine();
        var totalPct = report.TotalMarginPct.HasValue ? $"{report.TotalMarginPct.Value:0.0}%" : "N/A";
        sb.AppendLine($"\"Total\",\"{report.TotalRevenue:N2}\",\"{report.TotalCogs:N2}\"," +
                      $"\"{report.TotalGrossProfit:N2}\",\"{totalPct}\"");

        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Net Margin PDF
    // ────────────────────────────────────────────────────────────────────────

    public byte[] ToPdf(NetMarginReport report, GrossMarginExportMeta meta)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(ts => ts.FontSize(9).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    var logo = _logo.Value;
                    col.Item().Row(row =>
                    {
                        if (logo.Length > 0) row.ConstantItem(50).Image(logo);
                        row.RelativeItem().PaddingLeft(logo.Length > 0 ? 10 : 0).Column(inner =>
                        {
                            inner.Item().Text("Net Margin Analysis").FontSize(16).Bold();
                            inner.Item().Text($"{report.From:dd/MM/yyyy} – {report.To:dd/MM/yyyy}")
                                 .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    // Column header row
                    col.Item().Table(hdrTable =>
                    {
                        hdrTable.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(85); c.ConstantColumn(85); c.ConstantColumn(85);
                            c.ConstantColumn(85); c.ConstantColumn(85); c.ConstantColumn(65);
                        });

                        static IContainer HdrCell(IContainer c) => c
                            .BorderBottom(0.8f, Unit.Point).BorderColor(Colors.Grey.Darken1)
                            .PaddingVertical(4).PaddingHorizontal(4).AlignMiddle();

                        hdrTable.Cell().Element(HdrCell).Text("Product / Service Line").Bold();
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Revenue").Bold();
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("COGS").Bold();
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Gross Profit").Bold();
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Alloc. OpEx").Bold();
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Net Profit").Bold();
                        hdrTable.Cell().Element(HdrCell).AlignRight().Text("Net Margin %").Bold();
                    });

                    col.Item().Table(dataTable =>
                    {
                        dataTable.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(85); c.ConstantColumn(85); c.ConstantColumn(85);
                            c.ConstantColumn(85); c.ConstantColumn(85); c.ConstantColumn(65);
                        });

                        foreach (var row in report.Rows)
                        {
                            var bg      = row.BelowThreshold ? "#FAECE8" : "#FFFFFF";
                            var textCol = row.BelowThreshold ? SignalHex  : "#000000";
                            var netCol  = row.NetProfit < 0   ? SignalHex  : "#000000";
                            var netPct  = row.NetMarginPct.HasValue ? $"{row.NetMarginPct.Value:0.0}%" : "N/A";

                            IContainer DataCell(IContainer c) => c
                                .BorderBottom(0.3f, Unit.Point).BorderColor(Colors.Grey.Lighten2)
                                .Background(bg).Padding(4).AlignMiddle();

                            dataTable.Cell().Element(DataCell).Text(row.Name).FontColor(textCol);
                            dataTable.Cell().Element(DataCell).AlignRight().Text(row.Revenue.ToString("N2")).FontColor(textCol);
                            dataTable.Cell().Element(DataCell).AlignRight().Text(row.Cogs.ToString("N2")).FontColor(textCol);
                            dataTable.Cell().Element(DataCell).AlignRight().Text(row.GrossProfit.ToString("N2")).FontColor(textCol);
                            dataTable.Cell().Element(DataCell).AlignRight().Text(row.AllocatedOpEx.ToString("N2")).FontColor(textCol);
                            dataTable.Cell().Element(DataCell).AlignRight().Text(row.NetProfit.ToString("N2")).FontColor(netCol);
                            dataTable.Cell().Element(DataCell).AlignRight().Text(netPct).FontColor(netCol);
                        }
                    });

                    var totNetPct = report.TotalNetMarginPct.HasValue
                        ? $"{report.TotalNetMarginPct.Value:0.0}%"
                        : "N/A";
                    var totColour = report.TotalNetProfit > 0 ? GreenHex
                        : report.TotalNetProfit < 0 ? SignalHex
                        : StoneHex;

                    col.Item().BorderTop(1, Unit.Point).BorderColor(Colors.Grey.Darken1)
                       .Background(Colors.Grey.Lighten4).Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text("Total").Bold().FontSize(10).FontColor(totColour);
                        row.ConstantItem(85).AlignRight().Text(report.TotalRevenue.ToString("N2")).Bold().FontSize(10).FontColor(totColour);
                        row.ConstantItem(85).AlignRight().Text(report.TotalCogs.ToString("N2")).Bold().FontSize(10).FontColor(totColour);
                        row.ConstantItem(85).AlignRight().Text(report.TotalGrossProfit.ToString("N2")).Bold().FontSize(10).FontColor(totColour);
                        row.ConstantItem(85).AlignRight().Text(report.TotalAllocated.ToString("N2")).Bold().FontSize(10).FontColor(totColour);
                        row.ConstantItem(85).AlignRight().Text(report.TotalNetProfit.ToString("N2")).Bold().FontSize(10).FontColor(totColour);
                        row.ConstantItem(65).AlignRight().Text(totNetPct).Bold().FontSize(10).FontColor(totColour);
                    });
                });

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
            });
        }).GeneratePdf();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Net Margin CSV
    // ────────────────────────────────────────────────────────────────────────

    public string ToCsv(NetMarginReport report, GrossMarginExportMeta meta)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\"Net Margin Analysis — {report.From:dd/MM/yyyy} to {report.To:dd/MM/yyyy}\"");
        sb.AppendLine($"\"Generated by: {meta.GeneratedBy} on {meta.GeneratedAt:dd/MM/yyyy HH:mm} UTC\"");
        sb.AppendLine();
        sb.AppendLine("\"Product / Service Line\",\"Revenue\",\"COGS\",\"Gross Profit\",\"Allocated OpEx\",\"Net Profit\",\"Net Margin %\"");

        foreach (var row in report.Rows)
        {
            var netPct = row.NetMarginPct.HasValue ? $"{row.NetMarginPct.Value:0.0}%" : "N/A";
            sb.AppendLine($"\"{row.Name.Replace("\"", "\"\"")}\"," +
                          $"\"{row.Revenue:N2}\",\"{row.Cogs:N2}\"," +
                          $"\"{row.GrossProfit:N2}\",\"{row.AllocatedOpEx:N2}\"," +
                          $"\"{row.NetProfit:N2}\",\"{netPct}\"");
        }

        sb.AppendLine();
        var totalPct = report.TotalNetMarginPct.HasValue ? $"{report.TotalNetMarginPct.Value:0.0}%" : "N/A";
        sb.AppendLine($"\"Total\",\"{report.TotalRevenue:N2}\",\"{report.TotalCogs:N2}\"," +
                      $"\"{report.TotalGrossProfit:N2}\",\"{report.TotalAllocated:N2}\"," +
                      $"\"{report.TotalNetProfit:N2}\",\"{totalPct}\"");

        return sb.ToString();
    }
}
