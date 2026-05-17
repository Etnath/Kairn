using Kairn.Application.Common;
using Kairn.Domain.Entities;

namespace Kairn.Application.Features.GL;

public interface IAccountService
{
    Task<List<Account>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<Account>> CreateAsync(CreateAccountCommand cmd, CancellationToken ct = default);
    Task<Result<Account>> UpdateAsync(UpdateAccountCommand cmd, CancellationToken ct = default);
}
