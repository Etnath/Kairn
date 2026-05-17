using Kairn.Domain.Entities;

namespace Kairn.Application.Features.GL;

public record CreateAccountCommand(
    Guid TenantId,
    string Code,
    string Name,
    AccountType Type,
    Guid? ParentId,
    string Currency,
    bool IsActive);

public record UpdateAccountCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    Guid? ParentId,
    string Currency,
    bool IsActive);
