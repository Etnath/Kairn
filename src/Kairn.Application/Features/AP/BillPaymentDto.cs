using Kairn.Application.Common;
using Kairn.Domain.Entities;

namespace Kairn.Application.Features.AP;

public record BillPaymentDto(
    Guid Id,
    Guid BillId,
    DateOnly Date,
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    DateTimeOffset CreatedAt);

public record RecordBillPaymentCommand(
    Guid BillId,
    Guid TenantId,
    DateOnly Date,
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    Guid BankAccountId,
    string PostedByUserId,
    string PostedByName);

public record AchatEntryDto(
    DateOnly Date,
    string BillReference,
    string VendorName,
    decimal Amount,
    PaymentMethod Method,
    string? PaymentReference);

public interface IBillPaymentService
{
    Task<IReadOnlyList<BillPaymentDto>> GetByBillAsync(Guid billId, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<AchatEntryDto>> GetAchatsAsync(Guid tenantId, int year, CancellationToken ct = default);
    Task<Result<BillPaymentDto>> RecordAsync(RecordBillPaymentCommand cmd, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid paymentId, Guid tenantId, string deletedByUserId, string deletedByName, CancellationToken ct = default);
}
