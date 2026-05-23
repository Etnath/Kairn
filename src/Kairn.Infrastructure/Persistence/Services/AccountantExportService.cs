using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Kairn.Application.Features.Reports;
using Kairn.Application.Features.Tax;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class AccountantExportService(
    AppDbContext db,
    ITrialBalanceService trialBalanceService,
    ITrialBalanceExporter trialBalanceExporter,
    IPnlService pnlService,
    IPnlExporter pnlExporter,
    IBsService bsService,
    IBsExporter bsExporter,
    IVatReturnService vatReturnService,
    IVatReturnExporter vatReturnExporter) : IAccountantExportService
{
    public async Task<byte[]> GenerateZipAsync(
        Guid tenantId, DateOnly from, DateOnly to, string generatedBy,
        bool isAutoEntrepreneur = false,
        CancellationToken ct = default)
    {
        var period = $"{from:yyyy-MM-dd}_to_{to:yyyy-MM-dd}";
        var meta   = new PnlExportMeta(generatedBy, DateTimeOffset.UtcNow);

        using var ms      = new MemoryStream();
        using var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true);

        void AddBytes(string name, byte[] data)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using var s = entry.Open();
            s.Write(data, 0, data.Length);
        }

        void AddText(string name, string text)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using var s = entry.Open();
            using var w = new StreamWriter(s, Encoding.UTF8);
            w.Write(text);
        }

        if (isAutoEntrepreneur)
        {
            var pnlTask = pnlService.GenerateAsync(new PnlQuery(tenantId, from, to, HideZero: false), ct);
            var jeTask  = BuildJournalEntriesXlsxAsync(tenantId, from, to, ct);
            var lrTask  = BuildLivreDesRecettesCsvAsync(tenantId, from, to, ct);
            await Task.WhenAll(pnlTask, jeTask, lrTask);

            var pnl = pnlTask.Result;
            AddText ($"LivreDesRecettes_{period}.csv",   lrTask.Result);
            AddBytes($"ProfitLoss_{period}.pdf",          pnlExporter.ToPdf(pnl, meta));
            AddText ($"ProfitLoss_{period}.csv",          pnlExporter.ToCsv(pnl, meta));
            AddBytes($"JournalEntries_{period}.xlsx",     jeTask.Result);
        }
        else
        {
            var bsMeta  = new BsExportMeta(generatedBy, DateTimeOffset.UtcNow);
            var tbTask  = trialBalanceService.GenerateAsync(tenantId, to, ct);
            var pnlTask = pnlService.GenerateAsync(new PnlQuery(tenantId, from, to, HideZero: false), ct);
            var bsTask  = bsService.GenerateAsync(new BsQuery(tenantId, to), ct);
            var vatTask = vatReturnService.GenerateAsync(tenantId, from, to, ct);
            await Task.WhenAll(tbTask, pnlTask, bsTask, vatTask);

            var jeBytes = await BuildJournalEntriesXlsxAsync(tenantId, from, to, ct);

            AddBytes($"TrialBalance_{period}.pdf",    trialBalanceExporter.ToPdf(tbTask.Result));
            AddText ($"TrialBalance_{period}.csv",    trialBalanceExporter.ToCsv(tbTask.Result));
            AddBytes($"ProfitLoss_{period}.pdf",      pnlExporter.ToPdf(pnlTask.Result, meta));
            AddText ($"ProfitLoss_{period}.csv",      pnlExporter.ToCsv(pnlTask.Result, meta));
            AddBytes($"BalanceSheet_{period}.pdf",    bsExporter.ToPdf(bsTask.Result, bsMeta));
            AddText ($"BalanceSheet_{period}.csv",    bsExporter.ToCsv(bsTask.Result, bsMeta));
            AddBytes($"JournalEntries_{period}.xlsx", jeBytes);
            AddBytes($"VATSummary_{period}.pdf",      vatReturnExporter.ToPdf(vatTask.Result));
            AddText ($"VATSummary_{period}.csv",      vatReturnExporter.ToCsv(vatTask.Result));
        }

        archive.Dispose();
        return ms.ToArray();
    }

    private async Task<string> BuildLivreDesRecettesCsvAsync(
        Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var invoices = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Where(i => i.TenantId == tenantId
                     && !i.IsCreditNote
                     && i.Date >= from
                     && i.Date <= to
                     && i.Status != InvoiceStatus.Draft
                     && i.Status != InvoiceStatus.Void)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.Reference)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Date,Numéro,Client,Montant HT");
        foreach (var inv in invoices)
        {
            var net = inv.Lines.Sum(l => l.NetAmount);
            sb.AppendLine($"{inv.Date:dd/MM/yyyy},{CsvEscape(inv.Reference)},{CsvEscape(inv.Customer.Name)},{net:N2}");
        }
        return sb.ToString();
    }

    private static string CsvEscape(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;

    private async Task<byte[]> BuildJournalEntriesXlsxAsync(
        Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await db.JournalLines
            .Where(l => l.Entry.TenantId == tenantId
                     && l.Entry.Date >= from
                     && l.Entry.Date <= to)
            .OrderBy(l => l.Entry.Date)
            .ThenBy(l => l.Entry.Reference)
            .Select(l => new
            {
                l.Entry.Date,
                l.Entry.Reference,
                l.Entry.Description,
                AccountCode = l.Account.Code,
                AccountName = l.Account.Name,
                l.Debit,
                l.Credit,
                l.Currency,
                l.ExchangeRate,
                Memo = l.Memo ?? "",
            })
            .ToListAsync(ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Journal Entries");

        // Header row
        string[] headers = ["Date", "Reference", "Description", "Account Code",
            "Account Name", "Debit", "Credit", "Currency", "Exchange Rate", "Memo"];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value  = r.Date.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value  = r.Reference;
            ws.Cell(row, 3).Value  = r.Description;
            ws.Cell(row, 4).Value  = r.AccountCode;
            ws.Cell(row, 5).Value  = r.AccountName;
            ws.Cell(row, 6).Value  = (double)r.Debit;
            ws.Cell(row, 7).Value  = (double)r.Credit;
            ws.Cell(row, 8).Value  = r.Currency;
            ws.Cell(row, 9).Value  = (double)r.ExchangeRate;
            ws.Cell(row, 10).Value = r.Memo;

            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Style.NumberFormat.Format = "0.0000";
            row++;
        }

        ws.Columns().AdjustToContents(1, row - 1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
