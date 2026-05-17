using Kairn.Application.Common;
using Kairn.Application.Features.GL;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class JournalEntryService(AppDbContext db) : IJournalEntryService
{
    public async Task<PagedResult<JournalEntryDto>> GetPagedAsync(JournalEntryQuery query, CancellationToken ct = default)
    {
        var q = db.JournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .Where(e => e.TenantId == query.TenantId);

        if (query.From.HasValue) q = q.Where(e => e.Date >= query.From.Value);
        if (query.To.HasValue)   q = q.Where(e => e.Date <= query.To.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(e => e.Reference.ToLower().Contains(s) || e.Description.ToLower().Contains(s));
        }

        q = q.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<JournalEntryDto>(items.Select(ToDto).ToList(), total, query.Page, query.PageSize);
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
        entry.Lines = cmd.Lines.Select(l => new JournalLine
        {
            EntryId = entry.Id,
            AccountId = l.AccountId,
            Debit = l.Debit,
            Credit = l.Credit,
            Currency = l.Currency,
            ExchangeRate = l.ExchangeRate,
            Memo = l.Memo,
        }).ToList();

        await db.SaveChangesAsync(ct);

        var saved = await db.JournalEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstAsync(e => e.Id == entry.Id, ct);

        return Result<JournalEntryDto>.Ok(ToDto(saved));
    }

    private static string? ValidateLines(IReadOnlyList<JournalLineInput> lines)
    {
        if (lines.Count < 2) return "A journal entry must have at least 2 lines.";
        var debit = lines.Sum(l => l.Debit);
        var credit = lines.Sum(l => l.Credit);
        if (Math.Abs(debit - credit) >= 0.0001m) return "Debits and credits must be equal.";
        return null;
    }

    private static JournalEntryDto ToDto(JournalEntry e) => new(
        e.Id, e.Date, e.Reference, e.Description,
        e.Lines.Sum(l => l.Debit), e.Lines.Sum(l => l.Credit),
        e.CreatedByName, e.IsLocked, e.AttachmentFileName,
        e.Lines.Select(l => new JournalLineDto(
            l.Id, l.AccountId, l.Account.Code, l.Account.Name,
            l.Debit, l.Credit, l.Currency, l.ExchangeRate, l.Memo))
        .OrderBy(l => l.AccountCode).ToList());
}
