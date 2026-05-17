namespace Kairn.Application.Features.GL;

public record LedgerLineDto(
    Guid EntryId,
    DateOnly Date,
    string Reference,
    string Description,
    string CreatedByName,
    bool IsLocked,
    bool IsDeleted,
    bool IsRecurring,
    decimal Debit,
    decimal Credit,
    decimal? RunningBalance,
    string? AttachmentFileName,
    string? AttachmentPath,
    IReadOnlyList<JournalLineDto> Lines);

public record LedgerQuery(
    Guid TenantId,
    IReadOnlyList<Guid>? AccountIds = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Search = null,
    string? CreatedBy = null,
    int Page = 1,
    int PageSize = 50,
    bool ShowDeleted = false);
