using Kairn.Application.Common;
using Kairn.Application.Features.Tax;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class TaxPeriodService(AppDbContext db) : ITaxPeriodService
{
    private const string LockedError = "This transaction falls within a locked tax period.";

    public async Task<IReadOnlyList<TaxPeriodDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default) =>
        await db.TaxPeriods
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.StartDate)
            .Select(p => ToDto(p))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TaxPeriodDto>> GetOverlappingAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await db.TaxPeriods
            .Where(p => p.TenantId == tenantId && p.StartDate <= to && p.EndDate >= from)
            .OrderBy(p => p.StartDate)
            .Select(p => ToDto(p))
            .ToListAsync(ct);

    public async Task<bool> IsDateLockedAsync(Guid tenantId, DateOnly date, CancellationToken ct = default) =>
        await db.TaxPeriods.AnyAsync(
            p => p.TenantId == tenantId && p.IsLocked && p.StartDate <= date && p.EndDate >= date, ct);

    public async Task<Result<TaxPeriodDto>> CreateAsync(CreateTaxPeriodCommand cmd, CancellationToken ct = default)
    {
        if (cmd.EndDate < cmd.StartDate)
            return Result<TaxPeriodDto>.Fail("End date must be on or after start date.");

        var hasOverlap = await db.TaxPeriods.AnyAsync(
            p => p.TenantId == cmd.TenantId && p.StartDate <= cmd.EndDate && p.EndDate >= cmd.StartDate, ct);
        if (hasOverlap)
            return Result<TaxPeriodDto>.Fail("This period overlaps with an existing tax period.");

        var now = DateTimeOffset.UtcNow;
        var period = new TaxPeriod
        {
            TenantId  = cmd.TenantId,
            Name      = cmd.Name.Trim(),
            StartDate = cmd.StartDate,
            EndDate   = cmd.EndDate,
            IsLocked  = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.TaxPeriods.Add(period);
        await db.SaveChangesAsync(ct);
        return Result<TaxPeriodDto>.Ok(ToDto(period));
    }

    public async Task<IReadOnlyList<TaxPeriodDto>> GenerateQuartersAsync(Guid tenantId, int year, CancellationToken ct = default)
    {
        var quarters = new[]
        {
            (Name: $"Q1 {year}", Start: new DateOnly(year, 1, 1),  End: new DateOnly(year, 3, 31)),
            (Name: $"Q2 {year}", Start: new DateOnly(year, 4, 1),  End: new DateOnly(year, 6, 30)),
            (Name: $"Q3 {year}", Start: new DateOnly(year, 7, 1),  End: new DateOnly(year, 9, 30)),
            (Name: $"Q4 {year}", Start: new DateOnly(year, 10, 1), End: new DateOnly(year, 12, 31)),
        };

        var created = new List<TaxPeriodDto>();
        var now = DateTimeOffset.UtcNow;

        foreach (var q in quarters)
        {
            var exists = await db.TaxPeriods.AnyAsync(
                p => p.TenantId == tenantId && p.StartDate <= q.End && p.EndDate >= q.Start, ct);
            if (exists) continue;

            var period = new TaxPeriod
            {
                TenantId  = tenantId,
                Name      = q.Name,
                StartDate = q.Start,
                EndDate   = q.End,
                IsLocked  = false,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.TaxPeriods.Add(period);
            await db.SaveChangesAsync(ct);
            created.Add(ToDto(period));
        }

        return created;
    }

    public async Task<Result<TaxPeriodDto>> LockAsync(Guid id, Guid tenantId, string userId, string userName, CancellationToken ct = default)
    {
        var period = await db.TaxPeriods.FindAsync([id], ct);
        if (period is null || period.TenantId != tenantId)
            return Result<TaxPeriodDto>.Fail("Tax period not found.");

        if (period.IsLocked)
            return Result<TaxPeriodDto>.Fail("This tax period is already locked.");

        var now = DateTimeOffset.UtcNow;
        period.IsLocked         = true;
        period.LockedByUserId   = userId;
        period.LockedByUserName = userName;
        period.LockedAt         = now;
        period.UpdatedAt        = now;

        db.AuditLogs.Add(new AuditLog
        {
            EntityType = nameof(TaxPeriod),
            RecordId   = id.ToString(),
            Action     = "PeriodLocked",
            ChangedBy  = userName,
            ChangedAt  = now,
            NewValues  = $"{{\"LockedBy\":\"{userName}\",\"LockedAt\":\"{now:O}\"}}",
        });

        await db.SaveChangesAsync(ct);
        return Result<TaxPeriodDto>.Ok(ToDto(period));
    }

    public async Task<Result<TaxPeriodDto>> UnlockAsync(Guid id, Guid tenantId, string userId, string userName, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result<TaxPeriodDto>.Fail("A reason is required to unlock a tax period.");

        var period = await db.TaxPeriods.FindAsync([id], ct);
        if (period is null || period.TenantId != tenantId)
            return Result<TaxPeriodDto>.Fail("Tax period not found.");

        if (!period.IsLocked)
            return Result<TaxPeriodDto>.Fail("This tax period is not locked.");

        var now = DateTimeOffset.UtcNow;
        period.IsLocked         = false;
        period.LockedByUserId   = null;
        period.LockedByUserName = null;
        period.LockedAt         = null;
        period.UpdatedAt        = now;

        db.AuditLogs.Add(new AuditLog
        {
            EntityType = nameof(TaxPeriod),
            RecordId   = id.ToString(),
            Action     = "PeriodUnlocked",
            ChangedBy  = userName,
            ChangedAt  = now,
            NewValues  = $"{{\"UnlockedBy\":\"{userName}\",\"Reason\":\"{reason.Replace("\"", "\\\"")}\",\"UnlockedAt\":\"{now:O}\"}}",
        });

        await db.SaveChangesAsync(ct);
        return Result<TaxPeriodDto>.Ok(ToDto(period));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var period = await db.TaxPeriods.FindAsync([id], ct);
        if (period is null || period.TenantId != tenantId)
            return Result.Fail("Tax period not found.");

        if (period.IsLocked)
            return Result.Fail("Cannot delete a locked tax period. Unlock it first.");

        db.TaxPeriods.Remove(period);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static TaxPeriodDto ToDto(TaxPeriod p) =>
        new(p.Id, p.Name, p.StartDate, p.EndDate, p.IsLocked, p.LockedByUserName, p.LockedAt);
}
