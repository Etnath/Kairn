using Kairn.Application.Common;

namespace Kairn.Application.Features.GL;

public interface IRecurringEntryService
{
    Task<IReadOnlyList<RecurringEntryDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<RecurringEntryDto>> CreateAsync(CreateRecurringEntryCommand cmd, CancellationToken ct = default);
    Task<Result<RecurringEntryDto>> UpdateAsync(UpdateRecurringEntryCommand cmd, CancellationToken ct = default);
    Task<Result> SetActiveAsync(Guid id, Guid tenantId, bool active, CancellationToken ct = default);
    Task<Result<JournalEntryDto>> PostNowAsync(Guid id, Guid tenantId, string userId, string userName, CancellationToken ct = default);
    Task<IReadOnlyList<RecurringJobLogDto>> GetRecentErrorsAsync(Guid tenantId, int count = 10, CancellationToken ct = default);
    Task PostDueEntriesAsync(CancellationToken ct = default);
}
