using Kairn.Application.Features.Tax;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class VatReturnService(AppDbContext db) : IVatReturnService
{
    public async Task<VatReturnReport> GenerateAsync(
        Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var taxRates = await db.TaxRates
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(ct);
        var rateById = taxRates.ToDictionary(r => r.Id);

        // Output tax — regular invoices
        var invoiceLines = await db.InvoiceLines
            .Join(
                db.Invoices.Where(i =>
                    i.TenantId == tenantId &&
                    i.Status != InvoiceStatus.Draft &&
                    i.Status != InvoiceStatus.Void &&
                    i.Date >= from &&
                    i.Date <= to),
                l => l.InvoiceId,
                i => i.Id,
                (l, i) => new
                {
                    l.TaxRateId,
                    l.Quantity,
                    l.UnitPrice,
                    l.DiscountPct,
                    TaxRatePct = l.TaxRate,
                    i.IsCreditNote,
                })
            .ToListAsync(ct);

        // Input tax — bills
        var billLines = await db.BillLines
            .Join(
                db.Bills.Where(b =>
                    b.TenantId == tenantId &&
                    b.Status != BillStatus.Draft &&
                    b.Status != BillStatus.Void &&
                    b.Status != BillStatus.Rejected &&
                    b.Date >= from &&
                    b.Date <= to),
                l => l.BillId,
                b => b.Id,
                (l, b) => new
                {
                    l.TaxRateId,
                    l.Quantity,
                    l.UnitPrice,
                    TaxRatePct = l.TaxRate,
                })
            .ToListAsync(ct);

        // Accumulate per TaxRateId
        var outputByRate = new Dictionary<Guid?, (decimal Base, decimal Tax)>();
        foreach (var l in invoiceLines)
        {
            var gross   = l.Quantity * l.UnitPrice;
            var disc    = gross * l.DiscountPct / 100m;
            var net     = gross - disc;
            var taxAmt  = net * l.TaxRatePct / 100m;
            var sign    = l.IsCreditNote ? -1m : 1m;
            var key     = l.TaxRateId;
            outputByRate.TryGetValue(key, out var cur);
            outputByRate[key] = (cur.Base + sign * net, cur.Tax + sign * taxAmt);
        }

        var inputByRate = new Dictionary<Guid?, (decimal Base, decimal Tax)>();
        foreach (var l in billLines)
        {
            var net    = l.Quantity * l.UnitPrice;
            var taxAmt = net * l.TaxRatePct / 100m;
            var key    = l.TaxRateId;
            inputByRate.TryGetValue(key, out var cur);
            inputByRate[key] = (cur.Base + net, cur.Tax + taxAmt);
        }

        // Merge all keys
        var allKeys = outputByRate.Keys.Union(inputByRate.Keys).Distinct().ToList();

        var rows = allKeys
            .Select(key =>
            {
                outputByRate.TryGetValue(key, out var outAmt);
                inputByRate.TryGetValue(key, out var inAmt);

                string name;
                TaxCategory? cat;
                decimal rate;
                if (key.HasValue && rateById.TryGetValue(key.Value, out var tr))
                {
                    name = $"{tr.Name} ({tr.Rate:0.##}%)";
                    cat  = tr.Category;
                    rate = tr.Rate;
                }
                else
                {
                    name = "Non catégorisé";
                    cat  = null;
                    rate = 0m;
                }

                return new VatCategoryRow(key, name, cat, rate,
                    outAmt.Base, outAmt.Tax,
                    inAmt.Base,  inAmt.Tax);
            })
            .OrderBy(r => r.Rate == 0 ? 999 : 0)   // exempt last
            .ThenByDescending(r => r.Rate)
            .ToList();

        return new VatReturnReport(from, to, rows);
    }

    public async Task<IReadOnlyList<VatDrillDownItem>> GetDrillDownAsync(
        Guid tenantId, DateOnly from, DateOnly to,
        Guid? taxRateId, bool isOutput, CancellationToken ct = default)
    {
        if (isOutput)
        {
            var groups = await db.InvoiceLines
                .Join(
                    db.Invoices
                        .Include(i => i.Customer)
                        .Where(i =>
                            i.TenantId == tenantId &&
                            i.Status != InvoiceStatus.Draft &&
                            i.Status != InvoiceStatus.Void &&
                            i.Date >= from &&
                            i.Date <= to),
                    l => l.InvoiceId,
                    i => i.Id,
                    (l, i) => new
                    {
                        i.Id,
                        i.Reference,
                        i.Date,
                        CustomerName  = i.Customer.Name,
                        l.Quantity,
                        l.UnitPrice,
                        l.DiscountPct,
                        TaxRatePct    = l.TaxRate,
                        l.TaxRateId,
                        i.IsCreditNote,
                    })
                .Where(x => x.TaxRateId == taxRateId)
                .ToListAsync(ct);

            return groups
                .GroupBy(x => new { x.Id, x.Reference, x.Date, x.CustomerName, x.IsCreditNote })
                .Select(g =>
                {
                    var b   = g.Sum(l => { var gr = l.Quantity * l.UnitPrice; var d = gr * l.DiscountPct / 100m; return gr - d; });
                    var tax = g.Sum(l => { var gr = l.Quantity * l.UnitPrice; var d = gr * l.DiscountPct / 100m; var n = gr - d; return n * l.TaxRatePct / 100m; });
                    var sign = g.Key.IsCreditNote ? -1m : 1m;
                    return new VatDrillDownItem(g.Key.Id, g.Key.Reference, g.Key.Date,
                        g.Key.CustomerName, sign * b, sign * tax, g.Key.IsCreditNote);
                })
                .OrderBy(x => x.Date)
                .ToList();
        }
        else
        {
            var groups = await db.BillLines
                .Join(
                    db.Bills
                        .Include(b => b.Vendor)
                        .Where(b =>
                            b.TenantId == tenantId &&
                            b.Status != BillStatus.Draft &&
                            b.Status != BillStatus.Void &&
                            b.Status != BillStatus.Rejected &&
                            b.Date >= from &&
                            b.Date <= to),
                    l => l.BillId,
                    b => b.Id,
                    (l, b) => new
                    {
                        b.Id,
                        b.Reference,
                        b.Date,
                        VendorName = b.Vendor.Name,
                        l.Quantity,
                        l.UnitPrice,
                        TaxRatePct = l.TaxRate,
                        l.TaxRateId,
                    })
                .Where(x => x.TaxRateId == taxRateId)
                .ToListAsync(ct);

            return groups
                .GroupBy(x => new { x.Id, x.Reference, x.Date, x.VendorName })
                .Select(g =>
                {
                    var b   = g.Sum(l => l.Quantity * l.UnitPrice);
                    var tax = g.Sum(l => l.Quantity * l.UnitPrice * l.TaxRatePct / 100m);
                    return new VatDrillDownItem(g.Key.Id, g.Key.Reference, g.Key.Date,
                        g.Key.VendorName, b, tax, false);
                })
                .OrderBy(x => x.Date)
                .ToList();
        }
    }
}
