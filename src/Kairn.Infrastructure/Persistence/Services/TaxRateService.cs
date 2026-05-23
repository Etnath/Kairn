using Kairn.Application.Common;
using Kairn.Application.Features.Tax;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class TaxRateService(AppDbContext db) : ITaxRateService
{
    public async Task<IReadOnlyList<TaxRateDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await db.TaxRates
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Category)
            .ThenByDescending(t => t.ValidFrom)
            .Select(t => ToDto(t))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TaxRateDto>> GetActiveAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await db.TaxRates
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .OrderBy(t => t.Category)
            .ThenByDescending(t => t.Rate)
            .Select(t => ToDto(t))
            .ToListAsync(ct);
    }

    public async Task<TaxRateDto?> GetDefaultForDateAsync(Guid tenantId, TaxCategory category, DateOnly date, CancellationToken ct = default)
    {
        var rate = await db.TaxRates
            .Where(t => t.TenantId == tenantId && t.IsActive && t.IsDefault &&
                        t.Category == category &&
                        t.ValidFrom <= date &&
                        (t.ValidTo == null || t.ValidTo >= date))
            .FirstOrDefaultAsync(ct);

        return rate is null ? null : ToDto(rate);
    }

    public async Task<Result<TaxRateDto>> CreateAsync(CreateTaxRateCommand cmd, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        if (cmd.ValidTo.HasValue && cmd.ValidTo.Value < cmd.ValidFrom)
            return Result<TaxRateDto>.Fail("Valid To must be on or after Valid From.");

        if (cmd.IsDefault)
            await UnsetDefaultsAsync(cmd.TenantId, cmd.Category, null, ct);

        var rate = new TaxRate
        {
            TenantId  = cmd.TenantId,
            Name      = cmd.Name.Trim(),
            Rate      = cmd.Rate,
            Category  = cmd.Category,
            IsDefault = cmd.IsDefault,
            ValidFrom = cmd.ValidFrom,
            ValidTo   = cmd.ValidTo,
            IsActive  = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.TaxRates.Add(rate);
        await db.SaveChangesAsync(ct);
        return Result<TaxRateDto>.Ok(ToDto(rate));
    }

    public async Task<Result<TaxRateDto>> UpdateAsync(UpdateTaxRateCommand cmd, CancellationToken ct = default)
    {
        var rate = await db.TaxRates.FindAsync([cmd.Id], ct);
        if (rate is null || rate.TenantId != cmd.TenantId)
            return Result<TaxRateDto>.Fail("Tax rate not found.");

        if (await IsUsedInPostedTransactionAsync(cmd.Id, ct))
            return Result<TaxRateDto>.Fail("This tax rate has been applied to a posted transaction and cannot be edited. Create a new rate instead.");

        if (cmd.ValidTo.HasValue && cmd.ValidTo.Value < cmd.ValidFrom)
            return Result<TaxRateDto>.Fail("Valid To must be on or after Valid From.");

        if (cmd.IsDefault && !rate.IsDefault)
            await UnsetDefaultsAsync(cmd.TenantId, cmd.Category, cmd.Id, ct);

        rate.Name      = cmd.Name.Trim();
        rate.Rate      = cmd.Rate;
        rate.Category  = cmd.Category;
        rate.IsDefault = cmd.IsDefault;
        rate.ValidFrom = cmd.ValidFrom;
        rate.ValidTo   = cmd.ValidTo;
        rate.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<TaxRateDto>.Ok(ToDto(rate));
    }

    public async Task<Result> SetActiveAsync(Guid id, Guid tenantId, bool isActive, CancellationToken ct = default)
    {
        var rate = await db.TaxRates.FindAsync([id], ct);
        if (rate is null || rate.TenantId != tenantId)
            return Result.Fail("Tax rate not found.");

        rate.IsActive  = isActive;
        rate.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<bool> IsUsedInPostedTransactionAsync(Guid id, CancellationToken ct = default)
    {
        var usedInInvoice = await db.InvoiceLines
            .Where(l => l.TaxRateId == id)
            .Join(db.Invoices.Where(i => i.Status != InvoiceStatus.Draft),
                  l => l.InvoiceId, i => i.Id, (l, _) => l)
            .AnyAsync(ct);

        if (usedInInvoice) return true;

        return await db.BillLines
            .Where(l => l.TaxRateId == id)
            .Join(db.Bills.Where(b => b.Status != BillStatus.Draft && b.Status != BillStatus.Void && b.Status != BillStatus.Rejected),
                  l => l.BillId, b => b.Id, (l, _) => l)
            .AnyAsync(ct);
    }

    private async Task UnsetDefaultsAsync(Guid tenantId, TaxCategory category, Guid? excludeId, CancellationToken ct)
    {
        var existing = await db.TaxRates
            .Where(t => t.TenantId == tenantId && t.Category == category && t.IsDefault &&
                        (excludeId == null || t.Id != excludeId))
            .ToListAsync(ct);

        foreach (var t in existing)
        {
            t.IsDefault = false;
            t.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static TaxRateDto ToDto(TaxRate t) =>
        new(t.Id, t.Name, t.Rate, t.Category, t.IsDefault, t.ValidFrom, t.ValidTo, t.IsActive);
}
