using Kairn.Application.Common;
using Kairn.Application.Features.FixedAssets;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class FixedAssetService(AppDbContext db) : IFixedAssetService
{
    public async Task<IReadOnlyList<FixedAssetDto>> GetAllAsync(
        Guid tenantId, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = db.FixedAssets
            .Include(x => x.AssetAccount)
            .Include(x => x.AccumulatedDepreciationAccount)
            .Include(x => x.DepreciationExpenseAccount)
            .Where(x => x.TenantId == tenantId);

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        var assets = await query.OrderBy(x => x.Name).ToListAsync(ct);
        return assets.Select(ToDto).ToList();
    }

    public async Task<Result<FixedAssetDto>> CreateAsync(
        CreateFixedAssetCommand cmd, CancellationToken ct = default)
    {
        var asset = new FixedAsset
        {
            Id                               = Guid.NewGuid(),
            TenantId                         = cmd.TenantId,
            Name                             = cmd.Name.Trim(),
            Category                         = cmd.Category?.Trim(),
            PurchaseDate                     = cmd.PurchaseDate,
            PurchaseValue                    = cmd.PurchaseValue,
            ResidualValue                    = cmd.ResidualValue,
            Method                           = cmd.Method,
            UsefulLifeYears                  = cmd.UsefulLifeYears,
            AssetAccountId                   = cmd.AssetAccountId,
            AccumulatedDepreciationAccountId = cmd.AccumulatedDepreciationAccountId,
            DepreciationExpenseAccountId     = cmd.DepreciationExpenseAccountId,
            AccumulatedDepreciation          = 0m,
            HasDepreciationPostings          = false,
            IsFullyDepreciated               = false,
            IsActive                         = true,
        };

        db.FixedAssets.Add(asset);
        await db.SaveChangesAsync(ct);

        await db.Entry(asset).Reference(x => x.AssetAccount).LoadAsync(ct);
        await db.Entry(asset).Reference(x => x.AccumulatedDepreciationAccount).LoadAsync(ct);
        if (asset.DepreciationExpenseAccountId.HasValue)
            await db.Entry(asset).Reference(x => x.DepreciationExpenseAccount).LoadAsync(ct);

        return Result<FixedAssetDto>.Ok(ToDto(asset));
    }

    public async Task<Result<FixedAssetDto>> UpdateAsync(
        UpdateFixedAssetCommand cmd, CancellationToken ct = default)
    {
        var asset = await db.FixedAssets
            .Include(x => x.AssetAccount)
            .Include(x => x.AccumulatedDepreciationAccount)
            .Include(x => x.DepreciationExpenseAccount)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.TenantId == cmd.TenantId, ct);

        if (asset is null)
            return Result<FixedAssetDto>.Fail("Asset not found.");

        asset.Category                    = cmd.Category?.Trim();
        asset.AssetAccountId              = cmd.AssetAccountId;
        asset.AccumulatedDepreciationAccountId = cmd.AccumulatedDepreciationAccountId;
        asset.DepreciationExpenseAccountId    = cmd.DepreciationExpenseAccountId;

        if (!asset.HasDepreciationPostings)
        {
            asset.ResidualValue   = cmd.ResidualValue;
            asset.Method          = cmd.Method;
            asset.UsefulLifeYears = cmd.UsefulLifeYears;
        }

        await db.SaveChangesAsync(ct);

        await db.Entry(asset).Reference(x => x.AssetAccount).LoadAsync(ct);
        await db.Entry(asset).Reference(x => x.AccumulatedDepreciationAccount).LoadAsync(ct);
        if (asset.DepreciationExpenseAccountId.HasValue)
            await db.Entry(asset).Reference(x => x.DepreciationExpenseAccount).LoadAsync(ct);

        return Result<FixedAssetDto>.Ok(ToDto(asset));
    }

    public async Task<Result<FixedAssetDto>> DisposeAsync(
        DisposeFixedAssetCommand cmd, CancellationToken ct = default)
    {
        var asset = await db.FixedAssets
            .Include(x => x.AssetAccount)
            .Include(x => x.AccumulatedDepreciationAccount)
            .Include(x => x.DepreciationExpenseAccount)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.TenantId == cmd.TenantId, ct);

        if (asset is null)
            return Result<FixedAssetDto>.Fail("Asset not found.");
        if (!asset.IsActive)
            return Result<FixedAssetDto>.Fail("Asset has already been disposed.");
        if (asset.NetBookValue != 0m && cmd.GainLossAccountId is null)
            return Result<FixedAssetDto>.Fail("A gain/loss account is required when net book value is non-zero.");

        var reference = $"DISP-{cmd.DisposalDate:yyyyMM}-{asset.Id:N}"[..20];

        var je = new JournalEntry
        {
            Id              = Guid.NewGuid(),
            TenantId        = cmd.TenantId,
            Date            = cmd.DisposalDate,
            Reference       = reference,
            Description     = string.IsNullOrWhiteSpace(cmd.Notes)
                                ? $"Disposal of {asset.Name}"
                                : cmd.Notes.Trim(),
            CreatedByUserId = cmd.CreatedByUserId,
            CreatedByName   = cmd.CreatedByName,
            IsLocked        = true,
        };

        var lines = new List<JournalLine>();

        if (asset.AccumulatedDepreciation > 0m)
        {
            lines.Add(new JournalLine
            {
                Id        = Guid.NewGuid(),
                EntryId   = je.Id,
                AccountId = asset.AccumulatedDepreciationAccountId,
                Debit     = asset.AccumulatedDepreciation,
                Credit    = 0m,
                Currency  = "EUR",
                ExchangeRate = 1m,
            });
        }

        lines.Add(new JournalLine
        {
            Id        = Guid.NewGuid(),
            EntryId   = je.Id,
            AccountId = asset.AssetAccountId,
            Debit     = 0m,
            Credit    = asset.PurchaseValue,
            Currency  = "EUR",
            ExchangeRate = 1m,
        });

        var nbv = asset.NetBookValue;
        if (nbv != 0m && cmd.GainLossAccountId.HasValue)
        {
            if (nbv > 0m)
            {
                lines.Add(new JournalLine
                {
                    Id        = Guid.NewGuid(),
                    EntryId   = je.Id,
                    AccountId = cmd.GainLossAccountId.Value,
                    Debit     = nbv,
                    Credit    = 0m,
                    Currency  = "EUR",
                    ExchangeRate = 1m,
                });
            }
            else
            {
                lines.Add(new JournalLine
                {
                    Id        = Guid.NewGuid(),
                    EntryId   = je.Id,
                    AccountId = cmd.GainLossAccountId.Value,
                    Debit     = 0m,
                    Credit    = Math.Abs(nbv),
                    Currency  = "EUR",
                    ExchangeRate = 1m,
                });
            }
        }

        je.Lines = lines;
        db.JournalEntries.Add(je);

        asset.IsActive               = false;
        asset.DisposalDate           = cmd.DisposalDate;
        asset.DisposalJournalEntryId = je.Id;

        await db.SaveChangesAsync(ct);
        return Result<FixedAssetDto>.Ok(ToDto(asset));
    }

    public async Task<DepreciationRunResult> RunDepreciationAsync(
        Guid tenantId, DateOnly? upToMonth, string createdByUserId, string createdByName, CancellationToken ct = default)
    {
        var throughMonth = upToMonth ?? DateOnly.FromDateTime(DateTime.Today);

        var assets = await db.FixedAssets
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsFullyDepreciated)
            .ToListAsync(ct);

        int posted = 0, skipped = 0;
        var failures = new List<DepreciationAssetFailure>();

        foreach (var asset in assets)
        {
            if (asset.DepreciationExpenseAccountId is null)
            {
                skipped++;
                continue;
            }

            var pendingMonths = GetPendingMonths(asset, throughMonth).ToList();
            if (pendingMonths.Count == 0)
            {
                skipped++;
                continue;
            }

            var assetFailed = false;
            foreach (var month in pendingMonths)
            {
                var charge = CalculateMonthlyCharge(asset);
                var remaining = asset.NetBookValue - asset.ResidualValue;

                if (charge <= 0m || remaining <= 0m)
                {
                    asset.IsFullyDepreciated = true;
                    try { await db.SaveChangesAsync(ct); } catch { /* best-effort */ }
                    skipped++;
                    break;
                }

                if (charge > remaining)
                    charge = remaining;

                var periodKey    = month.ToString("yyyyMM");
                var lastDay      = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                var monthName    = new DateTime(month.Year, month.Month, 1).ToString("MMMM yyyy");

                try
                {
                    var existingCount = await db.JournalEntries
                        .CountAsync(x => x.TenantId == tenantId && x.Reference.StartsWith($"DEP-{periodKey}-"), ct);
                    var reference = $"DEP-{periodKey}-{existingCount + 1:D3}";

                    var je = new JournalEntry
                    {
                        Id              = Guid.NewGuid(),
                        TenantId        = tenantId,
                        Date            = lastDay,
                        Reference       = reference,
                        Description     = $"Depreciation — {asset.Name} — {monthName}",
                        CreatedByUserId = createdByUserId,
                        CreatedByName   = createdByName,
                        IsLocked        = true,
                        IsRecurring     = true,
                    };

                    je.Lines =
                    [
                        new JournalLine
                        {
                            Id           = Guid.NewGuid(),
                            EntryId      = je.Id,
                            AccountId    = asset.DepreciationExpenseAccountId.Value,
                            Debit        = charge,
                            Credit       = 0m,
                            Currency     = "EUR",
                            ExchangeRate = 1m,
                        },
                        new JournalLine
                        {
                            Id           = Guid.NewGuid(),
                            EntryId      = je.Id,
                            AccountId    = asset.AccumulatedDepreciationAccountId,
                            Debit        = 0m,
                            Credit       = charge,
                            Currency     = "EUR",
                            ExchangeRate = 1m,
                        },
                    ];

                    db.JournalEntries.Add(je);

                    asset.AccumulatedDepreciation += charge;
                    asset.LastDepreciatedDate      = lastDay;
                    asset.HasDepreciationPostings  = true;

                    if (asset.NetBookValue <= asset.ResidualValue)
                        asset.IsFullyDepreciated = true;

                    db.DepreciationLogs.Add(new DepreciationLog
                    {
                        TenantId        = tenantId,
                        AssetId         = asset.Id,
                        AssetName       = asset.Name,
                        Period          = new DateOnly(month.Year, month.Month, 1),
                        IsSuccess       = true,
                        PostedReference = reference,
                        AttemptedAt     = DateTimeOffset.UtcNow,
                    });

                    await db.SaveChangesAsync(ct);
                    posted++;

                    if (asset.IsFullyDepreciated) break;
                }
                catch (Exception ex)
                {
                    db.ChangeTracker.Clear();

                    db.DepreciationLogs.Add(new DepreciationLog
                    {
                        TenantId     = tenantId,
                        AssetId      = asset.Id,
                        AssetName    = asset.Name,
                        Period       = new DateOnly(month.Year, month.Month, 1),
                        IsSuccess    = false,
                        ErrorMessage = ex.Message,
                        AttemptedAt  = DateTimeOffset.UtcNow,
                    });
                    await db.SaveChangesAsync(ct);

                    failures.Add(new DepreciationAssetFailure(asset.Id, asset.Name, month, ex.Message));
                    assetFailed = true;
                    break;
                }
            }

            if (assetFailed)
            {
                // Re-attach remaining assets that haven't been processed yet
                var remaining = assets.SkipWhile(a => a.Id != asset.Id).Skip(1);
                foreach (var a in remaining)
                    if (db.Entry(a).State == EntityState.Detached)
                        db.Attach(a);
            }
        }

        return new DepreciationRunResult(posted, skipped, failures.Count, failures);
    }

    public async Task<int> GetRecentFailureCountAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await db.DepreciationLogs
            .CountAsync(x => x.TenantId == tenantId && x.IsSuccess == false, ct);
    }

    private static decimal CalculateMonthlyCharge(FixedAsset asset)
    {
        if (asset.Method == DepreciationMethod.StraightLine)
            return (asset.PurchaseValue - asset.ResidualValue) / (asset.UsefulLifeYears * 12m);

        // Declining balance: NBV × (1/usefulLifeYears) / 12
        var annualRate = 1.0m / asset.UsefulLifeYears;
        return asset.NetBookValue * annualRate / 12m;
    }

    private static IEnumerable<DateOnly> GetPendingMonths(FixedAsset asset, DateOnly throughMonth)
    {
        DateOnly start;
        if (asset.LastDepreciatedDate is null)
            start = new DateOnly(asset.PurchaseDate.Year, asset.PurchaseDate.Month, 1);
        else
        {
            var next = asset.LastDepreciatedDate.Value.AddMonths(1);
            start = new DateOnly(next.Year, next.Month, 1);
        }

        var through  = new DateOnly(throughMonth.Year, throughMonth.Month, 1);
        var current  = start;
        while (current <= through)
        {
            yield return current;
            current = current.AddMonths(1);
        }
    }

    private static FixedAssetDto ToDto(FixedAsset a) => new(
        a.Id,
        a.Name,
        a.Category,
        a.PurchaseDate,
        a.PurchaseValue,
        a.ResidualValue,
        a.Method,
        a.UsefulLifeYears,
        a.AssetAccountId,
        a.AssetAccount.Code,
        a.AssetAccount.Name,
        a.AccumulatedDepreciationAccountId,
        a.AccumulatedDepreciationAccount.Code,
        a.AccumulatedDepreciationAccount.Name,
        a.DepreciationExpenseAccountId,
        a.DepreciationExpenseAccount?.Code,
        a.DepreciationExpenseAccount?.Name,
        a.AccumulatedDepreciation,
        a.NetBookValue,
        a.LastDepreciatedDate,
        a.NextDepreciationDate,
        a.HasDepreciationPostings,
        a.IsFullyDepreciated,
        a.IsActive,
        a.DisposalDate);
}
