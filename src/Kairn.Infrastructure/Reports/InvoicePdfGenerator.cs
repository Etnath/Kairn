using System.Globalization;
using Kairn.Application.Common;
using Kairn.Application.Features.AR;
using Kairn.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Kairn.Infrastructure.Reports;

public sealed record InvoicePdfOptions(
    bool IsAutoEntrepreneur,
    string CompanyName,
    string? Siret,
    string? CompanyAddress);

public static class InvoicePdfGenerator
{
    public static byte[] Generate(InvoiceDto invoice, InvoicePdfOptions? options = null)
    {
        bool ae = options?.IsAutoEntrepreneur == true;
        var fr = new CultureInfo("fr-FR");
        string Money(decimal v) => MoneyFormatter.Format(v, invoice.Currency, fr);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(ts => ts.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(invoice.IsCreditNote ? "AVOIR" : "FACTURE").FontSize(22).Bold().FontColor("#1565C0");
                            c.Item().PaddingTop(4).Text($"# {invoice.Reference}").FontSize(12).FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(160).Column(c =>
                        {
                            c.Item().AlignRight().Text(options?.CompanyName ?? "Kairn").FontSize(13).Bold();
                            if (ae && !string.IsNullOrWhiteSpace(options?.Siret))
                                c.Item().AlignRight().Text($"SIRET: {options.Siret}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            if (ae && !string.IsNullOrWhiteSpace(options?.CompanyAddress))
                                c.Item().AlignRight().Text(options.CompanyAddress).FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().AlignRight().Text($"Date : {invoice.Date:dd/MM/yyyy}");
                            c.Item().AlignRight().Text($"Échéance : {invoice.DueDate:dd/MM/yyyy}");
                            if (StatusLabel(invoice.Status) is { } label)
                            c.Item().PaddingTop(4).AlignRight()
                                    .Text(label)
                                    .Bold()
                                    .FontColor(StatusColor(invoice.Status));
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    // Customer block
                    col.Item().Column(c =>
                    {
                        c.Item().Text("Facturer à").FontSize(9).FontColor(Colors.Grey.Darken1).Bold();
                        c.Item().PaddingTop(2).Text(invoice.CustomerName).Bold();
                        if (!string.IsNullOrWhiteSpace(invoice.CustomerEmail))
                            c.Item().Text(invoice.CustomerEmail).FontColor(Colors.Grey.Darken2);
                        if (!string.IsNullOrWhiteSpace(invoice.CustomerAddress))
                            c.Item().Text(invoice.CustomerAddress).FontColor(Colors.Grey.Darken2);
                        if (!string.IsNullOrWhiteSpace(invoice.CustomerTaxNumber))
                            c.Item().Text($"TVA: {invoice.CustomerTaxNumber}").FontColor(Colors.Grey.Darken2);
                    });

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);   // description
                            cols.ConstantColumn(55);  // qty
                            cols.ConstantColumn(80);  // unit price
                            cols.ConstantColumn(55);  // discount
                            if (!ae) cols.ConstantColumn(50);  // tax (hidden for auto-entrepreneur)
                            cols.ConstantColumn(80);  // total
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background("#1565C0").Padding(6).AlignMiddle();

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Description").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Qté").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Prix unitaire").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Remise %").FontColor(Colors.White).Bold();
                            if (!ae) header.Cell().Element(HeaderCell).AlignRight().Text("TVA %").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Total").FontColor(Colors.White).Bold();
                        });

                        bool even = false;
                        foreach (var line in invoice.Lines)
                        {
                            string bg = even ? Colors.Grey.Lighten4 : Colors.White;
                            even = !even;

                            IContainer DataCell(IContainer c) => c.Background(bg).Padding(5).AlignMiddle();

                            var gross = line.Quantity * line.UnitPrice;
                            var discount = gross * line.DiscountPct / 100m;
                            var net = gross - discount;
                            var lineTotal = ae ? net : net + net * line.TaxRate / 100m;

                            table.Cell().Element(DataCell).Text(line.Description);
                            table.Cell().Element(DataCell).AlignRight().Text(line.Quantity.ToString("N2", fr));
                            table.Cell().Element(DataCell).AlignRight().Text(Money(line.UnitPrice));
                            table.Cell().Element(DataCell).AlignRight().Text(line.DiscountPct > 0 ? $"{line.DiscountPct.ToString("N1", fr)} %" : "—");
                            if (!ae) table.Cell().Element(DataCell).AlignRight().Text(line.TaxRate > 0 ? $"{line.TaxRate.ToString("N1", fr)} %" : "0 %");
                            table.Cell().Element(DataCell).AlignRight().Text(Money(lineTotal));
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(12).AlignRight().Width(260).Column(totals =>
                    {
                        totals.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Sous-total HT").FontColor(Colors.Grey.Darken1);
                            row.ConstantItem(100).AlignRight().Text(Money(invoice.Subtotal));
                        });

                        if (invoice.TotalDiscount > 0)
                        {
                            totals.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Remise").FontColor(Colors.Grey.Darken1);
                                row.ConstantItem(100).AlignRight()
                                    .Text($"- {Money(invoice.TotalDiscount)}")
                                    .FontColor(Colors.Green.Darken2);
                            });
                        }

                        if (!ae && invoice.TotalTax > 0)
                        {
                            totals.Item().Row(row =>
                            {
                                row.RelativeItem().Text("TVA").FontColor(Colors.Grey.Darken1);
                                row.ConstantItem(100).AlignRight().Text(Money(invoice.TotalTax));
                            });
                        }

                        totals.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                        totals.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Total TTC").Bold();
                            row.ConstantItem(100).AlignRight()
                                .Text(Money(ae ? invoice.Subtotal - invoice.TotalDiscount : invoice.GrandTotal))
                                .Bold().FontColor("#1565C0");
                        });

                        if (ae)
                        {
                            totals.Item().PaddingTop(6).Text("TVA non applicable — art. 293 B du CGI")
                                .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                        }
                    });

                    // Notes
                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        col.Item().PaddingTop(20).Column(notes =>
                        {
                            notes.Item().Text("Notes / Mentions légales").Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
                            notes.Item().PaddingTop(4).Text(invoice.Notes).FontColor(Colors.Grey.Darken2);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(ts => ts.FontSize(8).FontColor(Colors.Grey.Medium));
                    text.Span("Page "); text.CurrentPageNumber(); text.Span(" sur "); text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static string? StatusLabel(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft        => "BROUILLON",
        InvoiceStatus.Sent         => null,
        InvoiceStatus.PartiallyPaid => "ACOMPTE REÇU",
        InvoiceStatus.Paid         => "PAYÉE",
        InvoiceStatus.Overdue      => "EN RETARD",
        InvoiceStatus.Void         => "ANNULÉE",
        _                          => null,
    };

    private static string StatusColor(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft        => Colors.Grey.Darken1,
        InvoiceStatus.Sent         => "#1565C0",
        InvoiceStatus.PartiallyPaid => "#E65100",
        InvoiceStatus.Paid         => Colors.Green.Darken2,
        InvoiceStatus.Overdue      => Colors.Red.Darken2,
        InvoiceStatus.Void         => Colors.Grey.Darken2,
        _                          => Colors.Grey.Darken1,
    };
}
