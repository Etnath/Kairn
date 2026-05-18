using Kairn.Application.Common;

namespace Kairn.Application.Features.AP;

public interface IVendorService
{
    Task<IReadOnlyList<VendorDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<VendorDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Result<VendorDto>> CreateAsync(CreateVendorCommand cmd, CancellationToken ct = default);
    Task<Result<VendorDto>> UpdateAsync(UpdateVendorCommand cmd, CancellationToken ct = default);
}
