using Kairn.Application.Features.Tenants;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class TenantMembershipService(AppDbContext db) : ITenantMembershipService
{
    public async Task<IReadOnlyList<TenantMembershipDto>> GetUserMembershipsAsync(
        string userId, CancellationToken ct = default) =>
        await db.TenantMemberships
            .Where(m => m.UserId == userId)
            .Include(m => m.Tenant)
            .Select(m => new TenantMembershipDto(m.TenantId, m.Tenant.Name, m.Role))
            .ToListAsync(ct);

    public async Task<bool> IsMemberAsync(string userId, Guid tenantId, CancellationToken ct = default) =>
        await db.TenantMemberships
            .AnyAsync(m => m.UserId == userId && m.TenantId == tenantId, ct);

    public async Task AddMemberAsync(Guid tenantId, string userId, TenantRole role, CancellationToken ct = default)
    {
        if (await IsMemberAsync(userId, tenantId, ct))
            return;
        db.TenantMemberships.Add(new TenantMembership
        {
            TenantId = tenantId,
            UserId   = userId,
            Role     = role,
            JoinedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}
