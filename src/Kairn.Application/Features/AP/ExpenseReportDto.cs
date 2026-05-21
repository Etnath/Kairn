using Kairn.Application.Common;
using Kairn.Domain.Entities;

namespace Kairn.Application.Features.AP;

public record ExpenseReportLineDto(
    Guid Id,
    string Description,
    DateOnly Date,
    decimal Amount,
    string Currency,
    Guid ExpenseAccountId,
    string ExpenseAccountName,
    bool HasReceipt,
    int SortOrder);

public record ExpenseReportDto(
    Guid Id,
    Guid TenantId,
    string Title,
    DateOnly SubmissionDate,
    string SubmittedByUserId,
    string SubmittedByName,
    ExpenseReportStatus Status,
    string Currency,
    decimal TotalAmount,
    IReadOnlyList<ExpenseReportLineDto> Lines,
    string? RejectionReason = null);

public record ExpenseReportLineInput(
    string Description,
    DateOnly Date,
    decimal Amount,
    string Currency,
    Guid ExpenseAccountId,
    int SortOrder = 0,
    byte[]? ReceiptData = null,
    string? ReceiptFileName = null,
    string? ReceiptContentType = null,
    bool RemoveReceipt = false);

public record ExpenseReportQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 25,
    ExpenseReportStatus? Status = null,
    bool SortDescending = true);

public record CreateExpenseReportCommand(
    Guid TenantId,
    string Title,
    DateOnly SubmissionDate,
    string Currency,
    string SubmittedByUserId,
    string SubmittedByName,
    IReadOnlyList<ExpenseReportLineInput> Lines);

public record UpdateExpenseReportCommand(
    Guid Id,
    Guid TenantId,
    string Title,
    DateOnly SubmissionDate,
    string Currency,
    IReadOnlyList<ExpenseReportLineInput> Lines);

public record ApproveExpenseReportCommand(
    Guid Id,
    Guid TenantId,
    string PostedByUserId,
    string PostedByName);

public record RejectExpenseReportCommand(
    Guid Id,
    Guid TenantId,
    string RejectionReason,
    string RejectedByUserId,
    string RejectedByName);

public record RecordExpensePaymentCommand(
    Guid Id,
    Guid TenantId,
    DateOnly Date,
    PaymentMethod Method,
    string? Reference,
    Guid BankAccountId,
    string PostedByUserId,
    string PostedByName);

public interface IExpenseReportService
{
    Task<PagedResult<ExpenseReportDto>> GetPagedAsync(ExpenseReportQuery query, CancellationToken ct = default);
    Task<ExpenseReportDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Result<ExpenseReportDto>> CreateAsync(CreateExpenseReportCommand cmd, CancellationToken ct = default);
    Task<Result<ExpenseReportDto>> UpdateAsync(UpdateExpenseReportCommand cmd, CancellationToken ct = default);
    Task<Result<ExpenseReportDto>> SubmitAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Result<ExpenseReportDto>> ApproveAsync(ApproveExpenseReportCommand cmd, CancellationToken ct = default);
    Task<Result<ExpenseReportDto>> RejectAsync(RejectExpenseReportCommand cmd, CancellationToken ct = default);
    Task<Result<ExpenseReportDto>> RecordPaymentAsync(RecordExpensePaymentCommand cmd, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<int> GetPendingApprovalCountAsync(Guid tenantId, CancellationToken ct = default);
    Task<(byte[] Data, string ContentType, string FileName)?> DownloadReceiptAsync(Guid lineId, Guid tenantId, CancellationToken ct = default);
}
