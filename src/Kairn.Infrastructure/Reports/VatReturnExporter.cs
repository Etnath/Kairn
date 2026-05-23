using Kairn.Application.Features.Tax;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Kairn.Infrastructure.Reports;

public class VatReturnExporter : IVatReturnExporter
{
    private static readonly string GreenHex = "#1F6040";
    private static readonly string RedHex   = "#7E2A14";

    public byte[] ToPdf(VatReturnReport report)
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
                    col.Item().Text("Déclaration de TVA").FontSize(14).Bold();
                    col.Item().Text($"Période : {report.From:dd/MM/yyyy} – {report.To:dd/MM/yyyy}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);  // Category
                            cols.RelativeColumn();   // Output Base
                            cols.RelativeColumn();   // Output Tax
                            cols.RelativeColumn();   // Input Base
                            cols.RelativeColumn();   // Input Tax
                            cols.RelativeColumn();   // Net VAT
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Colors.Blue.Lighten4).Padding(4).AlignMiddle();

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Catégorie").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Base sortante").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("TVA collectée").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Base entrante").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("TVA déductible").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("TVA nette").Bold();
                        });

                        static IContainer DataCell(IContainer c) =>
                            c.BorderBottom(0.3f, Unit.Point)
                             .BorderColor(Colors.Grey.Lighten2)
                             .Padding(3).AlignMiddle();

                        foreach (var row in report.Rows)
                        {
                            table.Cell().Element(DataCell).Text(row.TaxRateName);
                            table.Cell().Element(DataCell).AlignRight().Text(row.OutputBase.ToString("N2"));
                            table.Cell().Element(DataCell).AlignRight().Text(row.OutputTax.ToString("N2"));
                            table.Cell().Element(DataCell).AlignRight().Text(row.InputBase.ToString("N2"));
                            table.Cell().Element(DataCell).AlignRight().Text(row.InputTax.ToString("N2"));
                            var netColor = row.NetTax > 0 ? Colors.Red.Darken2 : row.NetTax < 0 ? Colors.Green.Darken2 : Colors.Black;
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(row.NetTax.ToString("N2")).FontColor(netColor);
                        }

                        // Totals row
                        static IContainer TotalCell(IContainer c) =>
                            c.Background(Colors.Grey.Lighten4).Padding(4).AlignMiddle();

                        table.Cell().Element(TotalCell).Text("Total").Bold();
                        table.Cell().Element(TotalCell).AlignRight().Text(report.TotalOutputBase.ToString("N2")).Bold();
                        table.Cell().Element(TotalCell).AlignRight().Text(report.TotalOutputTax.ToString("N2")).Bold();
                        table.Cell().Element(TotalCell).AlignRight().Text(report.TotalInputBase.ToString("N2")).Bold();
                        table.Cell().Element(TotalCell).AlignRight().Text(report.TotalInputTax.ToString("N2")).Bold();
                        var totalColor = report.NetVatPayable > 0 ? Colors.Red.Darken2
                                       : report.NetVatPayable < 0 ? Colors.Green.Darken2
                                       : Colors.Black;
                        table.Cell().Element(TotalCell).AlignRight()
                            .Text(report.NetVatPayable.ToString("N2")).Bold().FontColor(totalColor);
                    });

                    // Net VAT summary
                    var summaryColor = report.NetVatPayable > 0 ? RedHex
                                     : report.NetVatPayable < 0 ? GreenHex
                                     : "#8C8980";
                    var label = report.NetVatPayable >= 0 ? "TVA nette à payer" : "TVA à récupérer";
                    col.Item().PaddingTop(12).Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text(label).FontSize(12).Bold().FontColor(summaryColor);
                        row.ConstantItem(120).AlignRight()
                            .Text(Math.Abs(report.NetVatPayable).ToString("N2"))
                            .FontSize(12).Bold().FontColor(summaryColor);
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(ts => ts.FontSize(8).FontColor(Colors.Grey.Medium));
                    t.Span("Page "); t.CurrentPageNumber(); t.Span(" sur "); t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public string ToCsv(VatReturnReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Déclaration de TVA,Du {report.From:dd/MM/yyyy} au {report.To:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("Catégorie,Base sortante,TVA collectée,Base entrante,TVA déductible,TVA nette");

        foreach (var row in report.Rows)
        {
            sb.AppendLine(string.Join(',',
                CsvEscape(row.TaxRateName),
                row.OutputBase.ToString("N2"),
                row.OutputTax.ToString("N2"),
                row.InputBase.ToString("N2"),
                row.InputTax.ToString("N2"),
                row.NetTax.ToString("N2")));
        }

        sb.AppendLine(string.Join(',',
            "Total",
            report.TotalOutputBase.ToString("N2"),
            report.TotalOutputTax.ToString("N2"),
            report.TotalInputBase.ToString("N2"),
            report.TotalInputTax.ToString("N2"),
            report.NetVatPayable.ToString("N2")));

        sb.AppendLine();
        sb.AppendLine($"TVA nette à payer,{report.NetVatPayable.ToString("N2")}");

        return sb.ToString();
    }

    private static string CsvEscape(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
}
