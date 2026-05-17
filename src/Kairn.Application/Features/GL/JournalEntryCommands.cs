namespace Kairn.Application.Features.GL;

public record JournalLineInput(
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string Currency,
    decimal ExchangeRate,
    string? Memo);

public record CreateJournalEntryCommand(
    Guid TenantId,
    DateOnly Date,
    string Description,
    string CreatedByUserId,
    string CreatedByName,
    IReadOnlyList<JournalLineInput> Lines,
    string? AttachmentPath,
    string? AttachmentFileName);

public record UpdateJournalEntryCommand(
    Guid Id,
    Guid TenantId,
    DateOnly Date,
    string Description,
    IReadOnlyList<JournalLineInput> Lines,
    string? AttachmentPath,
    string? AttachmentFileName);

public record JournalEntryQuery(
    Guid TenantId,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 25);
