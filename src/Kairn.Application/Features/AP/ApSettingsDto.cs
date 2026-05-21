using Kairn.Application.Common;

namespace Kairn.Application.Features.AP;

public record ApSettingsDto(
    bool ApprovalEnabled,
    decimal ApprovalThreshold,
    string ApproverRoles);

public record SaveApSettingsCommand(
    Guid TenantId,
    bool ApprovalEnabled,
    decimal ApprovalThreshold,
    string ApproverRoles);

public interface IApSettingsService
{
    Task<ApSettingsDto> GetAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result> SaveAsync(SaveApSettingsCommand cmd, CancellationToken ct = default);
}
