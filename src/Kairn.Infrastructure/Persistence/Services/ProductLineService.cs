using Kairn.Application.Common;
using Kairn.Application.Features.MarginAnalysis;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class ProductLineService(AppDbContext db) : IProductLineService
{
    public async Task<List<ProductLineDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        var lines = await db.ProductLines
            .Include(p => p.Accounts)
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var accountIds = lines.SelectMany(p => p.Accounts.Select(a => a.AccountId)).Distinct().ToList();
        var accounts = accountIds.Count > 0
            ? await db.Accounts.Where(a => accountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct)
            : new Dictionary<Guid, Account>();

        return lines.Select(p => ToDto(p, accounts)).ToList();
    }

    public async Task<Result<ProductLineDto>> CreateAsync(CreateProductLineCommand cmd, CancellationToken ct = default)
    {
        if (await db.ProductLines.AnyAsync(
                p => p.TenantId == cmd.TenantId && p.Name == cmd.Name, ct))
            return Result<ProductLineDto>.Fail("Name already exists.");

        var line = new ProductLine
        {
            TenantId             = cmd.TenantId,
            Name                 = cmd.Name.Trim(),
            Description          = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim(),
            MarginAlertThreshold = cmd.MarginAlertThreshold,
            OpExAllocationPct    = cmd.OpExAllocationPct,
        };

        foreach (var id in cmd.RevenueAccountIds.Distinct())
            line.Accounts.Add(new ProductLineAccount { ProductLineId = line.Id, AccountId = id, Role = ProductLineAccountRole.Revenue });
        foreach (var id in cmd.CogsAccountIds.Distinct())
            line.Accounts.Add(new ProductLineAccount { ProductLineId = line.Id, AccountId = id, Role = ProductLineAccountRole.Cogs });

        db.ProductLines.Add(line);
        await db.SaveChangesAsync(ct);

        return Result<ProductLineDto>.Ok(await LoadDtoAsync(line.Id, ct));
    }

    public async Task<Result<ProductLineDto>> UpdateAsync(UpdateProductLineCommand cmd, CancellationToken ct = default)
    {
        var line = await db.ProductLines
            .Include(p => p.Accounts)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id && p.TenantId == cmd.TenantId, ct);
        if (line is null) return Result<ProductLineDto>.Fail("Product line not found.");

        if (await db.ProductLines.AnyAsync(
                p => p.TenantId == cmd.TenantId && p.Name == cmd.Name && p.Id != cmd.Id, ct))
            return Result<ProductLineDto>.Fail("Name already exists.");

        line.Name                 = cmd.Name.Trim();
        line.Description          = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        line.MarginAlertThreshold = cmd.MarginAlertThreshold;
        line.OpExAllocationPct    = cmd.OpExAllocationPct;
        line.IsActive             = cmd.IsActive;

        foreach (var a in line.Accounts.ToList())
            db.Entry(a).State = EntityState.Deleted;
        line.Accounts.Clear();

        foreach (var id in cmd.RevenueAccountIds.Distinct())
            line.Accounts.Add(new ProductLineAccount { ProductLineId = line.Id, AccountId = id, Role = ProductLineAccountRole.Revenue });
        foreach (var id in cmd.CogsAccountIds.Distinct())
            line.Accounts.Add(new ProductLineAccount { ProductLineId = line.Id, AccountId = id, Role = ProductLineAccountRole.Cogs });

        await db.SaveChangesAsync(ct);

        return Result<ProductLineDto>.Ok(await LoadDtoAsync(line.Id, ct));
    }

    private async Task<ProductLineDto> LoadDtoAsync(Guid id, CancellationToken ct)
    {
        var line = await db.ProductLines
            .Include(p => p.Accounts)
            .FirstAsync(p => p.Id == id, ct);
        var accountIds = line.Accounts.Select(a => a.AccountId).ToList();
        var accounts = accountIds.Count > 0
            ? await db.Accounts.Where(a => accountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct)
            : new Dictionary<Guid, Account>();
        return ToDto(line, accounts);
    }

    private static ProductLineDto ToDto(ProductLine p, Dictionary<Guid, Account> accounts)
    {
        ProductLineAccountDto Map(ProductLineAccount a) =>
            accounts.TryGetValue(a.AccountId, out var acc)
                ? new ProductLineAccountDto(acc.Id, acc.Code, acc.Name)
                : new ProductLineAccountDto(a.AccountId, "?", "Unknown");

        return new ProductLineDto(
            p.Id,
            p.Name,
            p.Description,
            p.MarginAlertThreshold,
            p.OpExAllocationPct,
            p.IsActive,
            p.Accounts.Where(a => a.Role == ProductLineAccountRole.Revenue).Select(Map).ToList(),
            p.Accounts.Where(a => a.Role == ProductLineAccountRole.Cogs).Select(Map).ToList());
    }
}
