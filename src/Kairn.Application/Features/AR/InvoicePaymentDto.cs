using Kairn.Application.Common;
using Kairn.Domain.Entities;

namespace Kairn.Application.Features.AR;

public record InvoicePaymentDto(
    Guid Id,
    Guid InvoiceId,
    DateOnly Date,
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    DateTimeOffset CreatedAt);

public record RecordPaymentCommand(
    Guid InvoiceId,
    Guid TenantId,
    DateOnly Date,
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    Guid BankAccountId,
    string PostedByUserId,
    string PostedByName);

public record RecetteEntryDto(
    DateOnly Date,
    string InvoiceReference,
    string CustomerName,
    decimal Amount,
    PaymentMethod Method,
    string? PaymentReference);

public interface IInvoicePaymentService
{
    Task<IReadOnlyList<InvoicePaymentDto>> GetByInvoiceAsync(Guid invoiceId, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<RecetteEntryDto>> GetRecettesAsync(Guid tenantId, int year, CancellationToken ct = default);
    Task<int?> GetEarliestPaymentYearAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<InvoicePaymentDto>> RecordAsync(RecordPaymentCommand cmd, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid paymentId, Guid tenantId, string deletedByUserId, string deletedByName, CancellationToken ct = default);
}
