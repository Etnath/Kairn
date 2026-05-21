using Kairn.Application.Features.AP;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Kairn.Infrastructure.Reports;

public class ApAgingExporter : IApAgingExporter
{
    private static readonly string[] Headers =
        ["Vendor", "Current", "1–30 days", "31–60 days", "61–90 days", "91+ days", "Total"];

    public byte[] ToPdf(ApAgingReportDto report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(ts => ts.FontSize(8).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text("Balance âgée des dettes fournisseurs")
                        .FontSize(14).Bold();
                    col.Item().Text($"Au : {report.AsOf:dd/MM/yyyy}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3);
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    static IContainer HeaderCell(IContainer c) =>
                        c.Background(Colors.Orange.Lighten4).Padding(4).AlignMiddle();

                    table.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text(Headers[0]).Bold();
                        for (int i = 1; i < Headers.Length; i++)
                            h.Cell().Element(HeaderCell).AlignRight().Text(Headers[i]).Bold();
                    });

                    static IContainer DataCell(IContainer c) =>
                        c.BorderBottom(0.5f, Unit.Point)
                         .BorderColor(Colors.Grey.Lighten2)
                         .Padding(3).AlignMiddle();

                    foreach (var row in report.Rows)
                    {
                        table.Cell().Element(DataCell).Text(row.VendorName);
                        table.Cell().Element(DataCell).AlignRight().Text(FormatAmount(row.Current));
                        table.Cell().Element(DataCell).AlignRight().Text(FormatAmount(row.Days1To30));
                        table.Cell().Element(DataCell).AlignRight().Text(FormatAmount(row.Days31To60));
                        table.Cell().Element(DataCell).AlignRight().Text(FormatAmount(row.Days61To90));
                        table.Cell().Element(DataCell).AlignRight().Text(FormatAmount(row.Days91Plus));
                        table.Cell().Element(DataCell).AlignRight()
                            .Text(row.Total.ToString("N2")).Bold();
                    }

                    static IContainer TotalCell(IContainer c) =>
                        c.Background(Colors.Grey.Lighten3).Padding(4).AlignMiddle();

                    table.Cell().Element(TotalCell).Text("TOTAL").Bold();
                    table.Cell().Element(TotalCell).AlignRight().Text(report.TotalCurrent.ToString("N2")).Bold();
                    table.Cell().Element(TotalCell).AlignRight().Text(report.TotalDays1To30.ToString("N2")).Bold();
                    table.Cell().Element(TotalCell).AlignRight().Text(report.TotalDays31To60.ToString("N2")).Bold();
                    table.Cell().Element(TotalCell).AlignRight().Text(report.TotalDays61To90.ToString("N2")).Bold();
                    table.Cell().Element(TotalCell).AlignRight().Text(report.TotalDays91Plus.ToString("N2")).Bold();
                    table.Cell().Element(TotalCell).AlignRight().Text(report.GrandTotal.ToString("N2")).Bold();
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(ts => ts.FontSize(7).FontColor(Colors.Grey.Medium));
                    text.Span("Page "); text.CurrentPageNumber(); text.Span(" / "); text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public string ToCsv(ApAgingReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\"Balance âgée des dettes fournisseurs – Au {report.AsOf:dd/MM/yyyy}\"");
        sb.AppendLine("\"Fournisseur\",\"Courant\",\"1-30 jours\",\"31-60 jours\",\"61-90 jours\",\"91+ jours\",\"Total\"");

        foreach (var row in report.Rows)
        {
            sb.AppendLine(
                $"\"{EscapeCsv(row.VendorName)}\"," +
                $"\"{row.Current:N2}\"," +
                $"\"{row.Days1To30:N2}\"," +
                $"\"{row.Days31To60:N2}\"," +
                $"\"{row.Days61To90:N2}\"," +
                $"\"{row.Days91Plus:N2}\"," +
                $"\"{row.Total:N2}\"");
        }

        sb.AppendLine(
            $"\"TOTAL\"," +
            $"\"{report.TotalCurrent:N2}\"," +
            $"\"{report.TotalDays1To30:N2}\"," +
            $"\"{report.TotalDays31To60:N2}\"," +
            $"\"{report.TotalDays61To90:N2}\"," +
            $"\"{report.TotalDays91Plus:N2}\"," +
            $"\"{report.GrandTotal:N2}\"");

        return sb.ToString();
    }

    private static string FormatAmount(decimal value) =>
        value != 0 ? value.ToString("N2") : "";

    private static string EscapeCsv(string s) => s.Replace("\"", "\"\"");
}
