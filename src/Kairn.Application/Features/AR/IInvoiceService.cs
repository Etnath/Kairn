using Kairn.Application.Common;

namespace Kairn.Application.Features.AR;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default);
    Task<InvoiceDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceCommand cmd, CancellationToken ct = default);
    Task<Result<InvoiceDto>> UpdateAsync(UpdateInvoiceCommand cmd, CancellationToken ct = default);
    Task<Result<InvoiceDto>> SendAsync(SendInvoiceCommand cmd, CancellationToken ct = default);
    Task<Result> VoidAsync(VoidInvoiceCommand cmd, CancellationToken ct = default);
    Task<byte[]?> GeneratePdfAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<string> GenerateReferenceAsync(Guid tenantId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceDto>> GetCreditNotesAsync(Guid originalInvoiceId, Guid tenantId, CancellationToken ct = default);
    Task<Result<InvoiceDto>> IssueCreditNoteAsync(IssueCreditNoteCommand cmd, CancellationToken ct = default);
    Task MarkAllOverdueAsync(CancellationToken ct = default);
    Task<(int Count, decimal TotalOutstanding)> GetOverdueSummaryAsync(Guid tenantId, CancellationToken ct = default);
}
