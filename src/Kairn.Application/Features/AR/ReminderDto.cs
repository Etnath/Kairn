using Kairn.Application.Common;

namespace Kairn.Application.Features.AR;

public record InvoiceReminderDto(
    Guid Id,
    DateOnly SentDate,
    string SentByName,
    string Method,
    DateTimeOffset CreatedAt);

public record SendReminderCommand(
    Guid InvoiceId,
    Guid TenantId,
    string MessageText,
    bool SendEmail,
    string SentByUserId,
    string SentByName);

public record BulkReminderRecipientDto(
    Guid CustomerId,
    string CustomerName,
    string? CustomerEmail,
    int InvoiceCount,
    decimal TotalOutstanding);

public record BulkReminderResultDto(
    int Sent,
    int Skipped,
    IReadOnlyList<string> Errors);

public record SendBulkReminderCommand(
    Guid TenantId,
    string SentByUserId,
    string SentByName);

public interface IReminderService
{
    Task<IReadOnlyList<InvoiceReminderDto>> GetByInvoiceAsync(
        Guid invoiceId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<BulkReminderRecipientDto>> GetBulkPreviewAsync(
        Guid tenantId, CancellationToken ct = default);

    Task<Result<InvoiceReminderDto>> SendAsync(
        SendReminderCommand cmd, CancellationToken ct = default);

    Task<BulkReminderResultDto> SendBulkAsync(
        SendBulkReminderCommand cmd, CancellationToken ct = default);
}

public interface IEmailService
{
    bool IsConfigured { get; }
    Task SendAsync(string to, string subject, string htmlBody,
        byte[]? attachment = null, string? attachmentName = null,
        CancellationToken ct = default);
}
