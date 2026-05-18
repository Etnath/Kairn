using Kairn.Domain.Entities;

namespace Kairn.Application.Features.AP;

public record BillLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    Guid ExpenseAccountId,
    string ExpenseAccountName,
    int SortOrder)
{
    public decimal NetAmount => Quantity * UnitPrice;
    public decimal TaxAmount => NetAmount * TaxRate / 100m;
    public decimal LineTotal => NetAmount + TaxAmount;
}

public record BillDto(
    Guid Id,
    Guid TenantId,
    Guid VendorId,
    string VendorName,
    string Reference,
    DateOnly Date,
    DateOnly DueDate,
    BillStatus Status,
    string Currency,
    string? Notes,
    decimal Subtotal,
    decimal TotalTax,
    decimal GrandTotal,
    decimal AmountPaid,
    IReadOnlyList<BillLineDto> Lines,
    bool HasAttachment)
{
    public decimal Outstanding => GrandTotal - AmountPaid;
}

public record BillLineInput(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    Guid ExpenseAccountId,
    int SortOrder = 0);

public record BillQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 25,
    Guid? VendorId = null,
    BillStatus? Status = null,
    bool ExcludeClosedStatuses = false);

public record CreateBillCommand(
    Guid TenantId,
    Guid VendorId,
    string Reference,
    DateOnly Date,
    DateOnly DueDate,
    string Currency,
    string? Notes,
    IReadOnlyList<BillLineInput> Lines,
    byte[]? AttachmentData = null,
    string? AttachmentFileName = null,
    string? AttachmentContentType = null);

public record UpdateBillCommand(
    Guid Id,
    Guid TenantId,
    Guid VendorId,
    string Reference,
    DateOnly Date,
    DateOnly DueDate,
    string Currency,
    string? Notes,
    IReadOnlyList<BillLineInput> Lines,
    bool RemoveAttachment = false,
    byte[]? AttachmentData = null,
    string? AttachmentFileName = null,
    string? AttachmentContentType = null);

public record ApproveBillCommand(
    Guid Id,
    Guid TenantId,
    string PostedByUserId,
    string PostedByName);

public record VoidBillCommand(
    Guid Id,
    Guid TenantId,
    string PostedByUserId,
    string PostedByName);
