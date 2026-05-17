using Kairn.Domain.Entities;

namespace Kairn.Application.Features.Reconciliation;

public record StartReconciliationCommand(
    Guid TenantId,
    Guid AccountId,
    DateOnly StartDate,
    DateOnly EndDate);

public record ImportLinesCommand(
    Guid SessionId,
    Guid TenantId,
    IReadOnlyList<ParsedTransaction> Lines,
    string FileName,
    StatementFormat Format);

public record MatchCommand(
    Guid SessionId,
    Guid TenantId,
    Guid BankLineId,
    IReadOnlyList<Guid> JournalLineIds,
    string MatchedByUserId);
