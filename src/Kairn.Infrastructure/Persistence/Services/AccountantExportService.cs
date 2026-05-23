using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Kairn.Application.Features.Reports;
using Kairn.Application.Features.Tax;
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
        CancellationToken ct = default)
    {
        var period = $"{from:yyyy-MM-dd}_to_{to:yyyy-MM-dd}";
        var meta   = new PnlExportMeta(generatedBy, DateTimeOffset.UtcNow);
        var bsMeta = new BsExportMeta(generatedBy, DateTimeOffset.UtcNow);

        // Run report generation in parallel where possible
        var tbTask  = trialBalanceService.GenerateAsync(tenantId, to, ct);
        var pnlTask = pnlService.GenerateAsync(new PnlQuery(tenantId, from, to, HideZero: false), ct);
        var bsTask  = bsService.GenerateAsync(new BsQuery(tenantId, to), ct);
        var vatTask = vatReturnService.GenerateAsync(tenantId, from, to, ct);

        await Task.WhenAll(tbTask, pnlTask, bsTask, vatTask);

        var tb  = await tbTask;
        var pnl = await pnlTask;
        var bs  = await bsTask;
        var vat = await vatTask;

        var jeBytes = await BuildJournalEntriesXlsxAsync(tenantId, from, to, ct);

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

        AddBytes($"TrialBalance_{period}.pdf",  trialBalanceExporter.ToPdf(tb));
        AddText ($"TrialBalance_{period}.csv",   trialBalanceExporter.ToCsv(tb));
        AddBytes($"ProfitLoss_{period}.pdf",     pnlExporter.ToPdf(pnl, meta));
        AddText ($"ProfitLoss_{period}.csv",     pnlExporter.ToCsv(pnl, meta));
        AddBytes($"BalanceSheet_{period}.pdf",   bsExporter.ToPdf(bs, bsMeta));
        AddText ($"BalanceSheet_{period}.csv",   bsExporter.ToCsv(bs, bsMeta));
        AddBytes($"JournalEntries_{period}.xlsx", jeBytes);
        AddBytes($"VATSummary_{period}.pdf",     vatReturnExporter.ToPdf(vat));
        AddText ($"VATSummary_{period}.csv",     vatReturnExporter.ToCsv(vat));

        archive.Dispose();
        return ms.ToArray();
    }

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
