using Kairn.Application.Common;

namespace Kairn.Application.Features.AR;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<CustomerDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerCommand cmd, CancellationToken ct = default);
    Task<Result<CustomerDto>> UpdateAsync(UpdateCustomerCommand cmd, CancellationToken ct = default);
}
