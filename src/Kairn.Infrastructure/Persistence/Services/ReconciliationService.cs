using Kairn.Application.Common;
using Kairn.Application.Features.Reconciliation;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class ReconciliationService(AppDbContext db) : IReconciliationService
{
    public async Task<ReconciliationSessionDto> StartSessionAsync(StartReconciliationCommand cmd, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new ReconciliationSession
        {
            TenantId = cmd.TenantId,
            AccountId = cmd.AccountId,
            StartDate = cmd.StartDate,
            EndDate = cmd.EndDate,
            Status = ReconciliationStatus.InProgress,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.ReconciliationSessions.Add(session);
        await db.SaveChangesAsync(ct);

        var full = await LoadSessionAsync(session.Id, ct);
        return full!;
    }

    public async Task<Result> ImportLinesAsync(ImportLinesCommand cmd, CancellationToken ct = default)
    {
        var session = await db.ReconciliationSessions
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId && s.TenantId == cmd.TenantId, ct);

        if (session is null) return Result.Fail("Session not found.");
        if (session.Status == ReconciliationStatus.Completed) return Result.Fail("Session already completed.");

        // Remove any previously imported lines for this session
        var existing = db.BankStatementLines.Where(l => l.SessionId == cmd.SessionId);
        db.BankStatementLines.RemoveRange(existing);

        var now = DateTimeOffset.UtcNow;
        var lines = cmd.Lines.Select(t => new BankStatementLine
        {
            TenantId = cmd.TenantId,
            SessionId = cmd.SessionId,
            Date = t.Date,
            Description = t.Description,
            Amount = t.Amount,
            ExternalId = t.ExternalId,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        db.BankStatementLines.AddRange(lines);
        session.StatementFileName = cmd.FileName;
        session.Format = cmd.Format;
        session.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<ReconciliationSessionDto?> GetSessionAsync(Guid sessionId, Guid tenantId, CancellationToken ct = default) =>
        await LoadSessionAsync(sessionId, ct, tenantId);

    public async Task<IReadOnlyList<LedgerLineForReconciliationDto>> GetUnreconciledLinesAsync(
        Guid tenantId, Guid accountId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var lines = await db.JournalLines
            .Where(l => l.AccountId == accountId && !l.IsReconciled)
            .Join(db.JournalEntries.Where(e => e.TenantId == tenantId && !e.IsDeleted && e.Date >= from && e.Date <= to),
                  l => l.EntryId, e => e.Id,
                  (l, e) => new LedgerLineForReconciliationDto(
                      l.Id, e.Id, e.Date, e.Reference, e.Description,
                      l.Debit, l.Credit, l.Memo))
            .OrderBy(x => x.EntryDate).ThenBy(x => x.Reference)
            .ToListAsync(ct);

        return lines;
    }

    public async Task<Result> MatchAsync(MatchCommand cmd, CancellationToken ct = default)
    {
        var session = await db.ReconciliationSessions
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId && s.TenantId == cmd.TenantId, ct);
        if (session is null) return Result.Fail("Session not found.");
        if (session.Status == ReconciliationStatus.Completed) return Result.Fail("Session is completed.");

        var bankLine = await db.BankStatementLines
            .FirstOrDefaultAsync(l => l.Id == cmd.BankLineId && l.SessionId == cmd.SessionId, ct);
        if (bankLine is null) return Result.Fail("Bank line not found.");

        var now = DateTimeOffset.UtcNow;
        foreach (var jlId in cmd.JournalLineIds)
        {
            // Avoid duplicate matches
            var alreadyMatched = await db.ReconciliationMatches
                .AnyAsync(m => m.BankLineId == cmd.BankLineId && m.JournalLineId == jlId, ct);
            if (alreadyMatched) continue;

            db.ReconciliationMatches.Add(new ReconciliationMatch
            {
                SessionId = cmd.SessionId,
                BankLineId = cmd.BankLineId,
                JournalLineId = jlId,
                MatchedAt = now,
                MatchedByUserId = cmd.MatchedByUserId,
            });
        }

        bankLine.IsMatched = true;
        bankLine.UpdatedAt = now;
        session.MatchedPairCount = await db.ReconciliationMatches
            .CountAsync(m => m.SessionId == cmd.SessionId, ct) + cmd.JournalLineIds.Count;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> UnmatchAsync(Guid sessionId, Guid tenantId, Guid bankLineId, CancellationToken ct = default)
    {
        var session = await db.ReconciliationSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId, ct);
        if (session is null) return Result.Fail("Session not found.");
        if (session.Status == ReconciliationStatus.Completed) return Result.Fail("Session is completed.");

        var bankLine = await db.BankStatementLines
            .FirstOrDefaultAsync(l => l.Id == bankLineId && l.SessionId == sessionId, ct);
        if (bankLine is null) return Result.Fail("Bank line not found.");

        var matches = db.ReconciliationMatches.Where(m => m.BankLineId == bankLineId);
        db.ReconciliationMatches.RemoveRange(matches);

        bankLine.IsMatched = false;
        bankLine.UpdatedAt = DateTimeOffset.UtcNow;
        session.MatchedPairCount = Math.Max(0, session.MatchedPairCount - await matches.CountAsync(ct));

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> CompleteAsync(Guid sessionId, Guid tenantId, CancellationToken ct = default)
    {
        var session = await db.ReconciliationSessions
            .Include(s => s.BankLines)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId, ct);
        if (session is null) return Result.Fail("Session not found.");
        if (session.Status == ReconciliationStatus.Completed) return Result.Fail("Already completed.");

        var unmatchedAmount = session.BankLines
            .Where(l => !l.IsMatched)
            .Sum(l => l.Amount);

        if (Math.Abs(unmatchedAmount) >= 0.01m)
            return Result.Fail($"Cannot complete: unmatched balance is {unmatchedAmount:N2}.");

        // Mark all matched journal lines as reconciled
        var matchedLineIds = await db.ReconciliationMatches
            .Where(m => m.SessionId == sessionId)
            .Select(m => m.JournalLineId)
            .Distinct()
            .ToListAsync(ct);

        var journalLines = await db.JournalLines
            .Where(l => matchedLineIds.Contains(l.Id))
            .ToListAsync(ct);

        foreach (var jl in journalLines)
            jl.IsReconciled = true;

        var now = DateTimeOffset.UtcNow;
        session.Status = ReconciliationStatus.Completed;
        session.CompletedAt = now;
        session.UpdatedAt = now;
        session.MatchedPairCount = matchedLineIds.Count;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<IReadOnlyList<ReconciliationSessionSummaryDto>> GetSessionsAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await db.ReconciliationSessions
            .Include(s => s.Account)
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new ReconciliationSessionSummaryDto(
                s.Id, s.AccountId, s.Account.Code, s.Account.Name,
                s.StartDate, s.EndDate, s.Status,
                s.MatchedPairCount, s.CompletedAt))
            .ToListAsync(ct);
    }

    private async Task<ReconciliationSessionDto?> LoadSessionAsync(Guid sessionId, CancellationToken ct, Guid? tenantId = null)
    {
        var q = db.ReconciliationSessions
            .Include(s => s.Account)
            .Include(s => s.BankLines)
                .ThenInclude(l => l.Matches)
                    .ThenInclude(m => m.JournalLine)
                        .ThenInclude(jl => jl.Entry)
            .AsQueryable();

        if (tenantId.HasValue)
            q = q.Where(s => s.TenantId == tenantId.Value);

        var session = await q.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null) return null;

        var bankLineDtos = session.BankLines
            .OrderBy(l => l.Date)
            .Select(l => new BankStatementLineDto(
                l.Id, l.Date, l.Description, l.Amount, l.Currency, l.ExternalId, l.IsMatched,
                l.Matches.Select(m => new MatchDetailDto(
                    m.Id, m.JournalLineId,
                    m.JournalLine.Entry.Date,
                    m.JournalLine.Entry.Reference,
                    m.JournalLine.Debit,
                    m.JournalLine.Credit))
                .ToList()))
            .ToList();

        return new ReconciliationSessionDto(
            session.Id, session.AccountId,
            session.Account.Code, session.Account.Name,
            session.StartDate, session.EndDate,
            session.Status, session.StatementFileName, session.Format,
            session.CompletedAt, session.MatchedPairCount, bankLineDtos);
    }
}
