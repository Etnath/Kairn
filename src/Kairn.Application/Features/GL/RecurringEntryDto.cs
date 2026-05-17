using Kairn.Domain.Entities;

namespace Kairn.Application.Features.GL;

public record RecurringEntryLineDto(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    string Currency,
    string? Memo);

public record RecurringEntryDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string EntryDescription,
    RecurringFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive,
    DateOnly? LastPostedDate,
    DateOnly NextDueDate,
    IReadOnlyList<RecurringEntryLineDto> Lines);

public record RecurringJobLogDto(
    long Id,
    Guid TenantId,
    Guid? RecurringEntryId,
    string EntryName,
    DateTimeOffset AttemptedAt,
    bool IsSuccess,
    string? ErrorMessage,
    string? PostedReference);

public record RecurringEntryLineInput(
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string Currency,
    decimal ExchangeRate,
    string? Memo);

public record CreateRecurringEntryCommand(
    Guid TenantId,
    string Name,
    string EntryDescription,
    RecurringFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<RecurringEntryLineInput> Lines);

public record UpdateRecurringEntryCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    string EntryDescription,
    RecurringFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<RecurringEntryLineInput> Lines);
