namespace Kairn.Application.Common;

public interface ICurrentUserContext
{
    string UserId { get; }
    string UserName { get; }
    Guid TenantId { get; }
}
