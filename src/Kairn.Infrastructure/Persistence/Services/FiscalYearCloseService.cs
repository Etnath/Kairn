using Kairn.Application.Common;
using Kairn.Application.Features.Equity;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class FiscalYearCloseService(AppDbContext db) : IFiscalYearCloseService
{
    public async Task<IReadOnlyList<int>> GetClosedYearsAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await db.FiscalYearCloses
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.FiscalYear)
            .Select(x => x.FiscalYear)
            .ToListAsync(ct);
    }

    public async Task<Result<FiscalYearCloseDto>> CloseYearAsync(CloseYearCommand cmd, CancellationToken ct = default)
    {
        if (cmd.FiscalYear >= DateTimeOffset.UtcNow.Year)
            return Result<FiscalYearCloseDto>.Fail("Only past fiscal years can be closed.");

        var alreadyClosed = await db.FiscalYearCloses
            .AnyAsync(x => x.TenantId == cmd.TenantId && x.FiscalYear == cmd.FiscalYear, ct);
        if (alreadyClosed)
            return Result<FiscalYearCloseDto>.Fail($"Fiscal year {cmd.FiscalYear} has already been closed.");

        var yearStart = new DateOnly(cmd.FiscalYear, 1, 1);
        var yearEnd   = new DateOnly(cmd.FiscalYear, 12, 31);

        // Compute net P&L for the year (credit - debit across all Revenue and Expense accounts)
        var plAccountIds = await db.Accounts
            .Where(a => a.TenantId == cmd.TenantId &&
                        (a.Type == AccountType.Revenue || a.Type == AccountType.Expense))
            .Select(a => a.Id)
            .ToListAsync(ct);

        decimal net = 0m;
        if (plAccountIds.Count > 0)
        {
            net = await db.JournalLines
                .Join(db.JournalEntries.Where(e =>
                        e.TenantId == cmd.TenantId &&
                        !e.IsDeleted &&
                        e.Date >= yearStart &&
                        e.Date <= yearEnd),
                    l => l.EntryId, e => e.Id,
                    (l, _) => l)
                .Where(l => plAccountIds.Contains(l.AccountId))
                .SumAsync(l => (l.Credit - l.Debit) * l.ExchangeRate, ct);
        }

        var now = DateTimeOffset.UtcNow;
        Guid? journalEntryId = null;

        if (net != 0m)
        {
            // Find required equity accounts by code
            var equityAccounts = await db.Accounts
                .Where(a => a.TenantId == cmd.TenantId &&
                            a.Type == AccountType.Equity &&
                            (a.Code == "110000" || a.Code == "119000" ||
                             a.Code == "120000" || a.Code == "129000"))
                .ToListAsync(ct);

            var acct110000 = equityAccounts.FirstOrDefault(a => a.Code == "110000");
            var acct119000 = equityAccounts.FirstOrDefault(a => a.Code == "119000");
            var acct120000 = equityAccounts.FirstOrDefault(a => a.Code == "120000");
            var acct129000 = equityAccounts.FirstOrDefault(a => a.Code == "129000");

            if (net > 0 && (acct120000 is null || acct110000 is null))
                return Result<FiscalYearCloseDto>.Fail("Required equity accounts (120000, 110000) not found.");
            if (net < 0 && (acct110000 is null || acct129000 is null))
                return Result<FiscalYearCloseDto>.Fail("Required equity accounts (110000, 129000) not found.");

            var amount    = Math.Abs(net);
            var closeDate = new DateOnly(cmd.FiscalYear, 12, 31);
            var reference = $"CLOT-{cmd.FiscalYear}";

            var je = new JournalEntry
            {
                Id              = Guid.NewGuid(),
                TenantId        = cmd.TenantId,
                Date            = closeDate,
                Reference       = reference,
                Description     = $"Clôture exercice {cmd.FiscalYear}",
                CreatedByUserId = cmd.UserId,
                CreatedByName   = cmd.UserName,
                IsLocked        = true,
                IsRecurring     = true,
                CreatedAt       = now,
                UpdatedAt       = now,
            };

            // Profit: DR 120000 (result), CR 110000 (retained earnings)
            // Loss:   DR 110000 (retained earnings absorbed), CR 129000 (loss carried)
            if (net > 0)
            {
                je.Lines =
                [
                    new JournalLine { AccountId = acct120000!.Id, Debit = amount, Credit = 0m, Currency = "EUR", ExchangeRate = 1m },
                    new JournalLine { AccountId = acct110000!.Id, Debit = 0m, Credit = amount, Currency = "EUR", ExchangeRate = 1m },
                ];
            }
            else
            {
                je.Lines =
                [
                    new JournalLine { AccountId = acct110000!.Id, Debit = amount, Credit = 0m, Currency = "EUR", ExchangeRate = 1m },
                    new JournalLine { AccountId = acct129000!.Id, Debit = 0m, Credit = amount, Currency = "EUR", ExchangeRate = 1m },
                ];
            }

            db.JournalEntries.Add(je);
            journalEntryId = je.Id;
        }

        var close = new FiscalYearClose
        {
            Id             = Guid.NewGuid(),
            TenantId       = cmd.TenantId,
            FiscalYear     = cmd.FiscalYear,
            JournalEntryId = journalEntryId,
            ClosedByUserId = cmd.UserId,
            ClosedByName   = cmd.UserName,
            CreatedAt      = now,
            UpdatedAt      = now,
        };

        db.FiscalYearCloses.Add(close);
        await db.SaveChangesAsync(ct);

        return Result<FiscalYearCloseDto>.Ok(new FiscalYearCloseDto(close.Id, close.FiscalYear, close.ClosedByName, close.CreatedAt));
    }
}
