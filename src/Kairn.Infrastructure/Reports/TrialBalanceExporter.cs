using Kairn.Application.Features.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Kairn.Infrastructure.Reports;

public class TrialBalanceExporter : ITrialBalanceExporter
{
    public byte[] ToPdf(TrialBalanceReport report)
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
                    col.Item().Text("Trial Balance")
                        .FontSize(16).Bold();
                    col.Item().Text($"As of: {report.AsOf:dd/MM/yyyy}")
                        .FontSize(10).FontColor(Colors.Grey.Medium);

                    if (!report.IsBalanced)
                    {
                        col.Item().PaddingTop(4).Background(Colors.Red.Lighten3).Padding(6)
                            .Text("⚠ Ledger is out of balance — please investigate.")
                            .FontColor(Colors.Red.Darken3).Bold();
                    }
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(70);   // code
                        cols.RelativeColumn();     // name
                        cols.ConstantColumn(100);  // debit
                        cols.ConstantColumn(100);  // credit
                    });

                    // Header
                    static IContainer HeaderCell(IContainer c) =>
                        c.Background(Colors.Grey.Lighten2).Padding(4).AlignMiddle();

                    table.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Code").Bold();
                        h.Cell().Element(HeaderCell).Text("Name").Bold();
                        h.Cell().Element(HeaderCell).AlignRight().Text("Debit").Bold();
                        h.Cell().Element(HeaderCell).AlignRight().Text("Credit").Bold();
                    });

                    // Rows
                    foreach (var row in report.Rows)
                    {
                        static IContainer DataCell(IContainer c) => c.BorderBottom(0.5f, Unit.Point)
                            .BorderColor(Colors.Grey.Lighten2).Padding(3).AlignMiddle();

                        table.Cell().Element(DataCell).Text(row.AccountCode);
                        table.Cell().Element(DataCell).Text(row.AccountName);
                        table.Cell().Element(DataCell).AlignRight()
                            .Text(row.DebitBalance > 0 ? row.DebitBalance.ToString("N2") : "");
                        table.Cell().Element(DataCell).AlignRight()
                            .Text(row.CreditBalance > 0 ? row.CreditBalance.ToString("N2") : "");
                    }

                    // Total row
                    static IContainer TotalCell(IContainer c) =>
                        c.Background(Colors.Grey.Lighten3).Padding(4).AlignMiddle();

                    table.Cell().Element(TotalCell).Text("TOTAL").Bold();
                    table.Cell().Element(TotalCell).Text("");
                    table.Cell().Element(TotalCell).AlignRight()
                        .Text(report.TotalDebit.ToString("N2")).Bold();
                    table.Cell().Element(TotalCell).AlignRight()
                        .Text(report.TotalCredit.ToString("N2")).Bold();
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(ts => ts.FontSize(8).FontColor(Colors.Grey.Medium));
                    text.Span("Page "); text.CurrentPageNumber(); text.Span(" / "); text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public string ToCsv(TrialBalanceReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\"Trial Balance as of {report.AsOf:dd/MM/yyyy}\"");
        sb.AppendLine("\"Account Code\",\"Account Name\",\"Debit Balance\",\"Credit Balance\"");

        foreach (var row in report.Rows)
        {
            sb.AppendLine(
                $"\"{row.AccountCode}\"," +
                $"\"{row.AccountName.Replace("\"", "\"\"")}\"," +
                $"\"{(row.DebitBalance > 0 ? row.DebitBalance.ToString("N2") : "")}\"," +
                $"\"{(row.CreditBalance > 0 ? row.CreditBalance.ToString("N2") : "")}\"");
        }

        sb.AppendLine(
            $"\"TOTAL\",\"\"," +
            $"\"{report.TotalDebit:N2}\"," +
            $"\"{report.TotalCredit:N2}\"");

        return sb.ToString();
    }
}
