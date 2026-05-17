using System.Text;
using Kairn.Application.Common;
using Kairn.Application.Features.GL;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class JournalEntryService(AppDbContext db) : IJournalEntryService
{
    public async Task<PagedResult<JournalEntryDto>> GetPagedAsync(JournalEntryQuery query, CancellationToken ct = default)
    {
        var baseQ = query.ShowDeleted
            ? db.JournalEntries.IgnoreQueryFilters().Where(e => e.IsDeleted)
            : db.JournalEntries.AsQueryable();

        var q = baseQ
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .Where(e => e.TenantId == query.TenantId);

        if (query.From.HasValue) q = q.Where(e => e.Date >= query.From.Value);
        if (query.To.HasValue)   q = q.Where(e => e.Date <= query.To.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(e => e.Reference.ToLower().Contains(s) || e.Description.ToLower().Contains(s));
        }

        q = q.OrderByDescending(e => e.Date).ThenByDescending(e => e.Reference);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<JournalEntryDto>(items.Select(ToDto).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<PagedResult<LedgerLineDto>> GetLedgerAsync(LedgerQuery query, CancellationToken ct = default)
    {
        bool singleAccount = query.AccountIds?.Count == 1;

        var baseQ = query.ShowDeleted
            ? db.JournalEntries.IgnoreQueryFilters().Where(e => e.IsDeleted)
            : db.JournalEntries.AsQueryable();

        var q = baseQ
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .Where(e => e.TenantId == query.TenantId);

        if (query.AccountIds is { Count: > 0 })
        {
            var ids = query.AccountIds;
            q = q.Where(e => e.Lines.Any(l => ids.Contains(l.AccountId)));
        }

        if (query.From.HasValue) q = q.Where(e => e.Date >= query.From.Value);
        if (query.To.HasValue)   q = q.Where(e => e.Date <= query.To.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(e => e.Reference.ToLower().Contains(s) || e.Description.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(query.CreatedBy))
        {
            var cb = query.CreatedBy.Trim().ToLower();
            q = q.Where(e => e.CreatedByName.ToLower().Contains(cb));
        }

        IOrderedQueryable<JournalEntry> ordered = singleAccount
            ? q.OrderBy(e => e.Date).ThenBy(e => e.Reference)
            : q.OrderByDescending(e => e.Date).ThenByDescending(e => e.Reference);

        var total = await ordered.CountAsync(ct);

        decimal balanceBefore = 0m;
        if (singleAccount)
        {
            var accountId = query.AccountIds![0];
            var skip = (query.Page - 1) * query.PageSize;
            if (skip > 0)
            {
                var beforeIds = await ordered.Take(skip).Select(e => e.Id).ToListAsync(ct);
                if (beforeIds.Count > 0)
                    balanceBefore = await db.JournalLines
                        .Where(l => beforeIds.Contains(l.EntryId) && l.AccountId == accountId)
                        .SumAsync(l => l.Debit - l.Credit, ct);
            }
        }

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var ledgerLines = new List<LedgerLineDto>(items.Count);
        var runningBalance = balanceBefore;

        foreach (var e in items)
        {
            decimal entryDebit, entryCredit;
            if (query.AccountIds is { Count: > 0 })
            {
                var accountIds = query.AccountIds;
                entryDebit  = e.Lines.Where(l => accountIds.Contains(l.AccountId)).Sum(l => l.Debit);
                entryCredit = e.Lines.Where(l => accountIds.Contains(l.AccountId)).Sum(l => l.Credit);
            }
            else
            {
                entryDebit  = e.Lines.Sum(l => l.Debit);
                entryCredit = e.Lines.Sum(l => l.Credit);
            }

            decimal? rb = null;
            if (singleAccount)
            {
                runningBalance += entryDebit - entryCredit;
                rb = runningBalance;
            }

            ledgerLines.Add(new LedgerLineDto(
                e.Id, e.Date, e.Reference, e.Description,
                e.CreatedByName, e.IsLocked, e.IsDeleted, e.IsRecurring,
                entryDebit, entryCredit, rb,
                e.AttachmentFileName, e.AttachmentPath,
                e.Lines.Select(l => new JournalLineDto(
                    l.Id, l.AccountId, l.Account.Code, l.Account.Name,
                    l.Debit, l.Credit, l.Currency, l.ExchangeRate, l.Memo, l.SystemRate))
                .OrderBy(l => l.AccountCode).ToList()));
        }

        return new PagedResult<LedgerLineDto>(ledgerLines, total, query.Page, query.PageSize);
    }

    public async Task<string> ExportLedgerCsvAsync(LedgerQuery query, CancellationToken ct = default)
    {
        var result = await GetLedgerAsync(query with { Page = 1, PageSize = 10_000 }, ct);
        bool hasAccountFilter = query.AccountIds is { Count: > 0 };

        var sb = new StringBuilder();
        if (hasAccountFilter)
            sb.AppendLine("Date,Reference,Description,Account Code,Account Name,Debit,Credit,Running Balance,Memo,Created By");
        else
            sb.AppendLine("Date,Reference,Description,Total Debit,Total Credit,Created By");

        foreach (var row in result.Items)
        {
            if (hasAccountFilter)
            {
                var accountIds = query.AccountIds!;
                foreach (var line in row.Lines.Where(l => accountIds.Contains(l.AccountId)))
                {
                    sb.AppendLine(string.Join(",",
                        row.Date.ToString("dd/MM/yyyy"),
                        CsvEscape(row.Reference),
                        CsvEscape(row.Description),
                        CsvEscape(line.AccountCode),
                        CsvEscape(line.AccountName),
                        line.Debit.ToString("N2"),
                        line.Credit.ToString("N2"),
                        row.RunningBalance?.ToString("N2") ?? "",
                        CsvEscape(line.Memo ?? ""),
                        CsvEscape(row.CreatedByName)));
                }
            }
            else
            {
                sb.AppendLine(string.Join(",",
                    row.Date.ToString("dd/MM/yyyy"),
                    CsvEscape(row.Reference),
                    CsvEscape(row.Description),
                    row.Debit.ToString("N2"),
                    row.Credit.ToString("N2"),
                    CsvEscape(row.CreatedByName)));
            }
        }

        return sb.ToString();
    }

    public async Task<JournalEntryDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var entry = await db.JournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);
        return entry is null ? null : ToDto(entry);
    }

    public async Task<string> GenerateReferenceAsync(Guid tenantId, DateOnly date, CancellationToken ct = default)
    {
        var prefix = $"JE-{date:yyyyMMdd}-";
        var count = await db.JournalEntries
            .IgnoreQueryFilters()
            .CountAsync(e => e.TenantId == tenantId && e.Reference.StartsWith(prefix), ct);
        return $"{prefix}{(count + 1):D3}";
    }

    public async Task<Result<JournalEntryDto>> CreateAsync(CreateJournalEntryCommand cmd, CancellationToken ct = default)
    {
        var validationError = ValidateLines(cmd.Lines);
        if (validationError is not null) return Result<JournalEntryDto>.Fail(validationError);

        var reference = await GenerateReferenceAsync(cmd.TenantId, cmd.Date, ct);
        var now = DateTimeOffset.UtcNow;

        var entry = new JournalEntry
        {
            TenantId = cmd.TenantId,
            Date = cmd.Date,
            Reference = reference,
            Description = cmd.Description,
            CreatedByUserId = cmd.CreatedByUserId,
            CreatedByName = cmd.CreatedByName,
            IsRecurring = cmd.IsRecurring,
            RecurringEntryId = cmd.RecurringEntryId,
            AttachmentPath = cmd.AttachmentPath,
            AttachmentFileName = cmd.AttachmentFileName,
            CreatedAt = now,
            UpdatedAt = now,
            Lines = cmd.Lines.Select(l => new JournalLine
            {
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                Currency = l.Currency,
                ExchangeRate = l.ExchangeRate,
                SystemRate = l.SystemRate,
                Memo = l.Memo,
            }).ToList(),
        };

        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        var saved = await db.JournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstAsync(e => e.Id == entry.Id, ct);

        return Result<JournalEntryDto>.Ok(ToDto(saved));
    }

    public async Task<Result<JournalEntryDto>> UpdateAsync(UpdateJournalEntryCommand cmd, CancellationToken ct = default)
    {
        var entry = await db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == cmd.Id && e.TenantId == cmd.TenantId, ct);

        if (entry is null) return Result<JournalEntryDto>.Fail("Journal entry not found.");
        if (entry.IsLocked) return Result<JournalEntryDto>.Fail("Journal entry is locked and cannot be edited.");

        var validationError = ValidateLines(cmd.Lines);
        if (validationError is not null) return Result<JournalEntryDto>.Fail(validationError);

        entry.Date = cmd.Date;
        entry.Description = cmd.Description;
        entry.AttachmentPath = cmd.AttachmentPath;
        entry.AttachmentFileName = cmd.AttachmentFileName;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        db.JournalLines.RemoveRange(entry.Lines);
        var newLines = cmd.Lines.Select(l => new JournalLine
        {
            EntryId = entry.Id,
            AccountId = l.AccountId,
            Debit = l.Debit,
            Credit = l.Credit,
            Currency = l.Currency,
            ExchangeRate = l.ExchangeRate,
            SystemRate = l.SystemRate,
            Memo = l.Memo,
        }).ToList();
        db.JournalLines.AddRange(newLines);

        await db.SaveChangesAsync(ct);

        var saved = await db.JournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstAsync(e => e.Id == entry.Id, ct);

        return Result<JournalEntryDto>.Ok(ToDto(saved));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var entry = await db.JournalEntries
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);

        if (entry is null) return Result.Fail("Journal entry not found.");
        if (entry.IsLocked) return Result.Fail("This entry is in a locked period.");

        entry.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> RestoreAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var entry = await db.JournalEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);

        if (entry is null) return Result.Fail("Journal entry not found.");

        entry.IsDeleted = false;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<int> GetDeletedCountAsync(Guid tenantId, DateOnly? from, DateOnly? to, string? search, CancellationToken ct = default)
    {
        var q = db.JournalEntries
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.IsDeleted);

        if (from.HasValue) q = q.Where(e => e.Date >= from.Value);
        if (to.HasValue)   q = q.Where(e => e.Date <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(e => e.Reference.ToLower().Contains(s) || e.Description.ToLower().Contains(s));
        }

        return await q.CountAsync(ct);
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static string? ValidateLines(IReadOnlyList<JournalLineInput> lines)
    {
        if (lines.Count < 2) return "A journal entry must have at least 2 lines.";
        // Validate in base currency (amount × exchange rate)
        var debit  = lines.Sum(l => l.Debit  * l.ExchangeRate);
        var credit = lines.Sum(l => l.Credit * l.ExchangeRate);
        if (Math.Abs(debit - credit) >= 0.01m) return "Debits and credits must be equal (base currency).";
        return null;
    }

    private static JournalEntryDto ToDto(JournalEntry e) => new(
        e.Id, e.Date, e.Reference, e.Description,
        e.Lines.Sum(l => l.Debit), e.Lines.Sum(l => l.Credit),
        e.CreatedByName, e.IsLocked, e.IsDeleted, e.AttachmentFileName,
        e.Lines.Select(l => new JournalLineDto(
            l.Id, l.AccountId, l.Account.Code, l.Account.Name,
            l.Debit, l.Credit, l.Currency, l.ExchangeRate, l.Memo, l.SystemRate))
        .OrderBy(l => l.AccountCode).ToList(),
        e.IsRecurring);
}
