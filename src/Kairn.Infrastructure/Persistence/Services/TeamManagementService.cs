using Kairn.Application.Common;
using Kairn.Application.Features.AR;
using Kairn.Application.Features.Tenants;
using Kairn.Domain.Entities;
using Kairn.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class TeamManagementService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService emailService) : ITeamManagementService
{
    public async Task<IReadOnlyList<TeamMemberDto>> GetMembersAsync(
        Guid tenantId, CancellationToken ct = default) =>
        await db.TenantMemberships
            .Where(m => m.TenantId == tenantId)
            .Join(db.Users,
                  m => m.UserId,
                  u => u.Id,
                  (m, u) => new TeamMemberDto(
                      u.Id,
                      u.DisplayName ?? u.Email ?? u.Id,
                      u.Email ?? "",
                      m.Role,
                      m.JoinedAt))
            .OrderBy(d => d.Role)
            .ThenBy(d => d.DisplayName)
            .ToListAsync(ct);

    public async Task<Result> InviteAsync(Guid tenantId, string email, TenantRole role,
        TenantRole actorRole, string actorName, CancellationToken ct = default)
    {
        if (!TeamPermissions.CanInvite(actorRole))
            return Result.Fail("You do not have permission to invite members.");

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Result.Fail("No Kairn account found for this email address.");

        if (await db.TenantMemberships.AnyAsync(
                m => m.TenantId == tenantId && m.UserId == user.Id, ct))
            return Result.Fail("This user is already a member of this company.");

        db.TenantMemberships.Add(new TenantMembership
        {
            TenantId = tenantId,
            UserId   = user.Id,
            Role     = role,
            JoinedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        if (emailService.IsConfigured)
        {
            var html = $"""
                <p>Hi {user.DisplayName ?? user.Email},</p>
                <p><strong>{actorName}</strong> has added you to a company on Kairn
                   with the role <strong>{role}</strong>.</p>
                <p><a href="/">Sign in to Kairn</a> to access your new company.</p>
                """;
            await emailService.SendAsync(email, "You've been added to a company on Kairn", html, ct: ct);
        }

        return Result.Ok();
    }

    public async Task<Result> ChangeRoleAsync(Guid tenantId, string targetUserId,
        TenantRole newRole, TenantRole actorRole, CancellationToken ct = default)
    {
        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == targetUserId, ct);

        if (membership is null)
            return Result.Fail("Member not found.");

        if (!TeamPermissions.CanChangeRole(actorRole, membership.Role, newRole, isSelf: false))
            return Result.Fail("You do not have permission to change this member's role.");

        membership.Role = newRole;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> RemoveAsync(Guid tenantId, string targetUserId,
        TenantRole actorRole, CancellationToken ct = default)
    {
        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == targetUserId, ct);

        if (membership is null)
            return Result.Fail("Member not found.");

        if (!TeamPermissions.CanRemove(actorRole, membership.Role, isSelf: false))
            return Result.Fail("You do not have permission to remove this member.");

        db.TenantMemberships.Remove(membership);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
