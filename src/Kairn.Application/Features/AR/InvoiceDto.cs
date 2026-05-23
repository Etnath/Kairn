using Kairn.Domain.Entities;

namespace Kairn.Application.Features.AR;

public record InvoiceDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerAddress,
    string? CustomerTaxNumber,
    string Reference,
    DateOnly Date,
    DateOnly DueDate,
    InvoiceStatus Status,
    string Currency,
    string? Notes,
    Guid? RevenueAccountId,
    decimal Subtotal,
    decimal TotalDiscount,
    decimal TotalTax,
    decimal GrandTotal,
    decimal AmountPaid,
    IReadOnlyList<InvoiceLineDto> Lines,
    bool IsCreditNote = false,
    Guid? OriginalInvoiceId = null,
    string? OriginalReference = null)
{
    public decimal Outstanding => GrandTotal - AmountPaid;
}

public record InvoiceLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPct,
    decimal TaxRate,
    int SortOrder,
    Guid? TaxRateId = null);

public record InvoiceQuery(
    Guid TenantId,
    Guid? CustomerId = null,
    InvoiceStatus? Status = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 25,
    bool ExcludeClosedStatuses = false);

public record InvoiceLineInput(
    string  Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPct,
    decimal TaxRate,
    int     SortOrder,
    Guid?   TaxRateId = null);

public record CreateInvoiceCommand(
    Guid TenantId,
    Guid CustomerId,
    string Reference,
    DateOnly Date,
    DateOnly DueDate,
    string Currency,
    string? Notes,
    Guid? RevenueAccountId,
    IReadOnlyList<InvoiceLineInput> Lines);

public record UpdateInvoiceCommand(
    Guid Id,
    Guid TenantId,
    Guid CustomerId,
    string Reference,
    DateOnly Date,
    DateOnly DueDate,
    string Currency,
    string? Notes,
    Guid? RevenueAccountId,
    IReadOnlyList<InvoiceLineInput> Lines);

public record SendInvoiceCommand(
    Guid Id,
    Guid TenantId,
    Guid RevenueAccountId,
    string PostedByUserId,
    string PostedByName);

public record VoidInvoiceCommand(
    Guid Id,
    Guid TenantId,
    string PostedByUserId,
    string PostedByName);

public record IssueCreditNoteCommand(
    Guid OriginalInvoiceId,
    Guid TenantId,
    DateOnly Date,
    string? Notes,
    Guid? RevenueAccountId,
    IReadOnlyList<InvoiceLineInput> Lines,
    string PostedByUserId,
    string PostedByName);
