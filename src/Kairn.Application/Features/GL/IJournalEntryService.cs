using Kairn.Application.Common;

namespace Kairn.Application.Features.GL;

public interface IJournalEntryService
{
    Task<PagedResult<JournalEntryDto>> GetPagedAsync(JournalEntryQuery query, CancellationToken ct = default);
    Task<PagedResult<LedgerLineDto>> GetLedgerAsync(LedgerQuery query, CancellationToken ct = default);
    Task<string> ExportLedgerCsvAsync(LedgerQuery query, CancellationToken ct = default);
    Task<JournalEntryDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<string> GenerateReferenceAsync(Guid tenantId, DateOnly date, CancellationToken ct = default);
    Task<Result<JournalEntryDto>> CreateAsync(CreateJournalEntryCommand cmd, CancellationToken ct = default);
    Task<Result<JournalEntryDto>> UpdateAsync(UpdateJournalEntryCommand cmd, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Result> RestoreAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<int> GetDeletedCountAsync(Guid tenantId, DateOnly? from, DateOnly? to, string? search, CancellationToken ct = default);
}
