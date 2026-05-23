using Kairn.Domain.Entities;

namespace Kairn.Application.Features.Tenants;

public record TenantMembershipDto(Guid TenantId, string TenantName, TenantRole Role);

public interface ITenantMembershipService
{
    Task<IReadOnlyList<TenantMembershipDto>> GetUserMembershipsAsync(string userId, CancellationToken ct = default);
    Task<bool> IsMemberAsync(string userId, Guid tenantId, CancellationToken ct = default);
    Task AddMemberAsync(Guid tenantId, string userId, TenantRole role, CancellationToken ct = default);
}
