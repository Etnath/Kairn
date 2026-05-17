namespace Kairn.Application.Features.GL;

public record JournalEntryDto(
    Guid Id,
    DateOnly Date,
    string Reference,
    string Description,
    decimal TotalDebit,
    decimal TotalCredit,
    string CreatedByName,
    bool IsLocked,
    bool IsDeleted,
    string? AttachmentFileName,
    IReadOnlyList<JournalLineDto> Lines,
    bool IsRecurring = false);

public record JournalLineDto(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    string Currency,
    decimal ExchangeRate,
    string? Memo,
    decimal? SystemRate = null);
