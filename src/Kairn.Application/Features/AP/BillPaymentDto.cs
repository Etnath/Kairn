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

public interface IBillPaymentService
{
    Task<IReadOnlyList<BillPaymentDto>> GetByBillAsync(Guid billId, Guid tenantId, CancellationToken ct = default);
    Task<Result<BillPaymentDto>> RecordAsync(RecordBillPaymentCommand cmd, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid paymentId, Guid tenantId, string deletedByUserId, string deletedByName, CancellationToken ct = default);
}
