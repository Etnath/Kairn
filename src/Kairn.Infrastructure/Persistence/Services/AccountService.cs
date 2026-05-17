using Kairn.Application.Common;
using Kairn.Application.Features.GL;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class AccountService : IAccountService
{
    private readonly AppDbContext _db;

    public AccountService(AppDbContext db) => _db = db;

    public Task<List<Account>> GetAllAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Accounts
           .Where(a => a.TenantId == tenantId)
           .OrderBy(a => a.Type)
           .ThenBy(a => a.Code)
           .ToListAsync(ct);

    public async Task<Result<Account>> CreateAsync(CreateAccountCommand cmd, CancellationToken ct = default)
    {
        if (await _db.Accounts.AnyAsync(a => a.TenantId == cmd.TenantId && a.Code == cmd.Code, ct))
            return Result<Account>.Fail("Account code already exists.");

        var account = new Account
        {
            TenantId = cmd.TenantId,
            Code = cmd.Code,
            Name = cmd.Name,
            Type = cmd.Type,
            ParentId = cmd.ParentId,
            Currency = cmd.Currency,
            IsActive = cmd.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(ct);
        return Result<Account>.Ok(account);
    }

    public async Task<Result<Account>> UpdateAsync(UpdateAccountCommand cmd, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == cmd.Id && a.TenantId == cmd.TenantId, ct);

        if (account is null)
            return Result<Account>.Fail("Account not found.");

        account.Name = cmd.Name;
        account.ParentId = cmd.ParentId;
        account.Currency = cmd.Currency;
        account.IsActive = cmd.IsActive;
        account.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result<Account>.Ok(account);
    }
}
