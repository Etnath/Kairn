using Kairn.Application.Common;

namespace Kairn.Application.Features.AP;

public interface IBillService
{
    Task<PagedResult<BillDto>> GetPagedAsync(BillQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<BillDto>> GetByVendorAsync(Guid vendorId, Guid tenantId, CancellationToken ct = default);
    Task<BillDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Result<BillDto>> CreateAsync(CreateBillCommand cmd, CancellationToken ct = default);
    Task<Result<BillDto>> UpdateAsync(UpdateBillCommand cmd, CancellationToken ct = default);
    Task<Result<BillDto>> ApproveAsync(ApproveBillCommand cmd, CancellationToken ct = default);
    Task<Result> VoidAsync(VoidBillCommand cmd, CancellationToken ct = default);
    Task MarkAllOverdueAsync(CancellationToken ct = default);
    Task<(byte[] Data, string FileName, string ContentType)?> DownloadAttachmentAsync(
        Guid billId, Guid attachmentId, Guid tenantId, CancellationToken ct = default);
}
