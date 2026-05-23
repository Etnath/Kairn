using Kairn.Application.Common;
using Kairn.Domain.Entities;

namespace Kairn.Application.Features.Tenants;

public record TeamMemberDto(
    string         UserId,
    string         DisplayName,
    string         Email,
    TenantRole     Role,
    DateTimeOffset JoinedAt);

public interface ITeamManagementService
{
    Task<IReadOnlyList<TeamMemberDto>> GetMembersAsync(Guid tenantId, CancellationToken ct = default);

    Task<Result> InviteAsync(Guid tenantId, string email, TenantRole role,
        TenantRole actorRole, string actorName, CancellationToken ct = default);

    Task<Result> ChangeRoleAsync(Guid tenantId, string targetUserId, TenantRole newRole,
        TenantRole actorRole, CancellationToken ct = default);

    Task<Result> RemoveAsync(Guid tenantId, string targetUserId,
        TenantRole actorRole, CancellationToken ct = default);
}

public static class TeamPermissions
{
    public static bool CanInvite(TenantRole actor) =>
        actor is TenantRole.Owner or TenantRole.Admin;

    public static bool CanChangeRole(TenantRole actor, TenantRole target, TenantRole newRole,
        bool isSelf)
    {
        if (isSelf) return false;
        if (actor == TenantRole.Owner)
            return target != TenantRole.Owner; // Owner cannot demote the sole other Owner
        if (actor == TenantRole.Admin)
            return target   is TenantRole.Member or TenantRole.ReadOnly
                && newRole  is TenantRole.Member or TenantRole.ReadOnly;
        return false;
    }

    public static bool CanRemove(TenantRole actor, TenantRole target, bool isSelf)
    {
        if (isSelf || target == TenantRole.Owner) return false;
        if (actor == TenantRole.Owner) return true;
        if (actor == TenantRole.Admin) return target is TenantRole.Member or TenantRole.ReadOnly;
        return false;
    }
}
