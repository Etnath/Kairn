using Kairn.Domain.Entities;

namespace Kairn.Application.Features.Reconciliation;

public record ReconciliationSessionSummaryDto(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    DateOnly StartDate,
    DateOnly EndDate,
    ReconciliationStatus Status,
    int MatchedPairCount,
    DateTimeOffset? CompletedAt);

public record ReconciliationSessionDto(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    DateOnly StartDate,
    DateOnly EndDate,
    ReconciliationStatus Status,
    string? StatementFileName,
    StatementFormat? Format,
    DateTimeOffset? CompletedAt,
    int MatchedPairCount,
    IReadOnlyList<BankStatementLineDto> BankLines);

public record BankStatementLineDto(
    Guid Id,
    DateOnly Date,
    string Description,
    decimal Amount,
    string Currency,
    string? ExternalId,
    bool IsMatched,
    IReadOnlyList<MatchDetailDto> Matches);

public record MatchDetailDto(
    Guid MatchId,
    Guid JournalLineId,
    DateOnly EntryDate,
    string Reference,
    decimal Debit,
    decimal Credit);

public record LedgerLineForReconciliationDto(
    Guid LineId,
    Guid EntryId,
    DateOnly EntryDate,
    string Reference,
    string EntryDescription,
    decimal Debit,
    decimal Credit,
    string? Memo);

public record ParsedTransaction(
    DateOnly Date,
    string Description,
    decimal Amount,
    string? ExternalId);

public record CsvColumnMapping(
    int DateColumnIndex,
    int DescriptionColumnIndex,
    int AmountColumnIndex,
    int? DebitColumnIndex,
    int? CreditColumnIndex,
    string Delimiter,
    bool HasHeaderRow,
    string DateFormat);
