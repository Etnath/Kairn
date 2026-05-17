using Kairn.Application.Common;
using Kairn.Application.Features.GL;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class RecurringEntryService(AppDbContext db, IJournalEntryService journalSvc, IExchangeRateService exRateSvc) : IRecurringEntryService
{
    public async Task<IReadOnlyList<RecurringEntryDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        var entries = await db.RecurringEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .Where(e => e.TenantId == tenantId)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);

        return entries.Select(ToDto).ToList();
    }

    public async Task<Result<RecurringEntryDto>> CreateAsync(CreateRecurringEntryCommand cmd, CancellationToken ct = default)
    {
        var err = ValidateLines(cmd.Lines);
        if (err is not null) return Result<RecurringEntryDto>.Fail(err);

        var now = DateTimeOffset.UtcNow;
        var entry = new RecurringEntry
        {
            TenantId = cmd.TenantId,
            Name = cmd.Name,
            EntryDescription = cmd.EntryDescription,
            Frequency = cmd.Frequency,
            StartDate = cmd.StartDate,
            EndDate = cmd.EndDate,
            IsActive = true,
            NextDueDate = cmd.StartDate,
            CreatedAt = now,
            UpdatedAt = now,
            Lines = cmd.Lines.Select(l => new RecurringEntryLine
            {
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                Currency = l.Currency,
                ExchangeRate = l.ExchangeRate,
                Memo = l.Memo,
            }).ToList(),
        };

        db.RecurringEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        var saved = await LoadWithLinesAsync(entry.Id, ct);
        return Result<RecurringEntryDto>.Ok(ToDto(saved!));
    }

    public async Task<Result<RecurringEntryDto>> UpdateAsync(UpdateRecurringEntryCommand cmd, CancellationToken ct = default)
    {
        var entry = await db.RecurringEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == cmd.Id && e.TenantId == cmd.TenantId, ct);

        if (entry is null) return Result<RecurringEntryDto>.Fail("Template not found.");

        var err = ValidateLines(cmd.Lines);
        if (err is not null) return Result<RecurringEntryDto>.Fail(err);

        entry.Name = cmd.Name;
        entry.EntryDescription = cmd.EntryDescription;
        entry.Frequency = cmd.Frequency;
        entry.StartDate = cmd.StartDate;
        entry.EndDate = cmd.EndDate;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        // If not yet posted, recalculate NextDueDate from new StartDate
        if (entry.LastPostedDate is null)
            entry.NextDueDate = cmd.StartDate;

        db.RecurringEntryLines.RemoveRange(entry.Lines);
        entry.Lines = cmd.Lines.Select(l => new RecurringEntryLine
        {
            RecurringEntryId = entry.Id,
            AccountId = l.AccountId,
            Debit = l.Debit,
            Credit = l.Credit,
            Currency = l.Currency,
            ExchangeRate = l.ExchangeRate,
            Memo = l.Memo,
        }).ToList();

        await db.SaveChangesAsync(ct);

        var saved = await LoadWithLinesAsync(entry.Id, ct);
        return Result<RecurringEntryDto>.Ok(ToDto(saved!));
    }

    public async Task<Result> SetActiveAsync(Guid id, Guid tenantId, bool active, CancellationToken ct = default)
    {
        var entry = await db.RecurringEntries
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);

        if (entry is null) return Result.Fail("Template not found.");

        entry.IsActive = active;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<JournalEntryDto>> PostNowAsync(Guid id, Guid tenantId, string userId, string userName, CancellationToken ct = default)
    {
        var entry = await LoadWithLinesAsync(id, ct);
        if (entry is null || entry.TenantId != tenantId)
            return Result<JournalEntryDto>.Fail("Template not found.");

        if (!entry.IsActive)
            return Result<JournalEntryDto>.Fail("Template is inactive.");

        return await PostEntryAsync(entry, userId, userName, ct);
    }

    public async Task<IReadOnlyList<RecurringJobLogDto>> GetRecentErrorsAsync(Guid tenantId, int count = 10, CancellationToken ct = default)
    {
        var logs = await db.RecurringJobLogs
            .Where(l => l.TenantId == tenantId && !l.IsSuccess)
            .OrderByDescending(l => l.Id)
            .Take(count)
            .ToListAsync(ct);

        return logs.Select(l => new RecurringJobLogDto(
            l.Id, l.TenantId, l.RecurringEntryId, l.EntryName,
            l.AttemptedAt, l.IsSuccess, l.ErrorMessage, l.PostedReference))
            .ToList();
    }

    public async Task PostDueEntriesAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var due = await db.RecurringEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .Where(e => e.IsActive && e.NextDueDate <= today
                        && (e.EndDate == null || today <= e.EndDate))
            .ToListAsync(ct);

        foreach (var entry in due)
        {
            await PostEntryAsync(entry, "system", "System", ct);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<Result<JournalEntryDto>> PostEntryAsync(
        RecurringEntry entry, string userId, string userName, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var description = $"[{entry.Name}] {entry.EntryDescription}";

        // Fetch current exchange rates for each line
        var lineInputs = new List<JournalLineInput>();
        foreach (var l in entry.Lines)
        {
            var rateResult = await exRateSvc.GetRateAsync(l.Currency, today, ct);
            lineInputs.Add(new JournalLineInput(
                l.AccountId, l.Debit, l.Credit, l.Currency,
                rateResult.Rate, l.Memo, rateResult.Rate));
        }

        var cmd = new CreateJournalEntryCommand(
            entry.TenantId,
            today,
            description,
            userId,
            userName,
            lineInputs,
            null, null,
            IsRecurring: true,
            RecurringEntryId: entry.Id);

        var result = await journalSvc.CreateAsync(cmd, ct);

        var log = new RecurringJobLog
        {
            TenantId = entry.TenantId,
            RecurringEntryId = entry.Id,
            EntryName = entry.Name,
            AttemptedAt = DateTimeOffset.UtcNow,
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.IsSuccess ? null : result.Error,
            PostedReference = result.IsSuccess ? result.Value!.Reference : null,
        };
        db.RecurringJobLogs.Add(log);

        if (result.IsSuccess)
        {
            entry.LastPostedDate = today;
            entry.NextDueDate = CalculateNextDue(today, entry.Frequency);
            entry.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return result;
    }

    private async Task<RecurringEntry?> LoadWithLinesAsync(Guid id, CancellationToken ct) =>
        await db.RecurringEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    internal static DateOnly CalculateNextDue(DateOnly from, RecurringFrequency freq) => freq switch
    {
        RecurringFrequency.Daily     => from.AddDays(1),
        RecurringFrequency.Weekly    => from.AddDays(7),
        RecurringFrequency.Monthly   => from.AddMonths(1),
        RecurringFrequency.Quarterly => from.AddMonths(3),
        RecurringFrequency.Annually  => from.AddYears(1),
        _                            => from.AddMonths(1),
    };

    private static string? ValidateLines(IReadOnlyList<RecurringEntryLineInput> lines)
    {
        if (lines.Count < 2) return "A journal entry requires at least 2 lines.";
        var debit  = lines.Sum(l => l.Debit);
        var credit = lines.Sum(l => l.Credit);
        if (Math.Abs(debit - credit) >= 0.0001m) return "Debits and credits must be equal.";
        return null;
    }

    private static RecurringEntryDto ToDto(RecurringEntry e) => new(
        e.Id, e.TenantId, e.Name, e.EntryDescription, e.Frequency,
        e.StartDate, e.EndDate, e.IsActive, e.LastPostedDate, e.NextDueDate,
        e.Lines.Select(l => new RecurringEntryLineDto(
            l.Id, l.AccountId, l.Account.Code, l.Account.Name,
            l.Debit, l.Credit, l.Currency, l.Memo))
        .OrderBy(l => l.AccountCode).ToList());
}
