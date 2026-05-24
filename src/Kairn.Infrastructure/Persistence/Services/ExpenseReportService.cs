using Kairn.Application.Common;
using Kairn.Application.Features.AP;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class ExpenseReportService(AppDbContext db) : IExpenseReportService
{
    private const string ApAccountCode = "401000";

    public async Task<PagedResult<ExpenseReportDto>> GetPagedAsync(ExpenseReportQuery query, CancellationToken ct = default)
    {
        var q = db.ExpenseReports
            .Include(e => e.Lines).ThenInclude(l => l.ExpenseAccount)
            .Where(e => e.TenantId == query.TenantId);

        if (query.Status.HasValue)
            q = q.Where(e => e.Status == query.Status.Value);

        q = query.SortDescending
            ? q.OrderByDescending(e => e.SubmissionDate).ThenByDescending(e => e.Id)
            : q.OrderBy(e => e.SubmissionDate).ThenBy(e => e.Id);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ExpenseReportDto>(
            items.Select(ToDto).ToList(),
            total, query.Page, query.PageSize);
    }

    public async Task<ExpenseReportDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var report = await db.ExpenseReports
            .Include(e => e.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);

        return report is null ? null : ToDto(report);
    }

    public async Task<Result<ExpenseReportDto>> CreateAsync(CreateExpenseReportCommand cmd, CancellationToken ct = default)
    {
        if (!cmd.Lines.Any())
            return Result<ExpenseReportDto>.Fail("An expense report must have at least one line.");

        var now = DateTimeOffset.UtcNow;
        var report = new ExpenseReport
        {
            TenantId = cmd.TenantId,
            Title = cmd.Title,
            SubmissionDate = cmd.SubmissionDate,
            Currency = cmd.Currency,
            SubmittedByUserId = cmd.SubmittedByUserId,
            SubmittedByName = cmd.SubmittedByName,
            Status = ExpenseReportStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };

        ApplyLines(report, cmd.Lines);

        db.ExpenseReports.Add(report);
        await db.SaveChangesAsync(ct);

        return Result<ExpenseReportDto>.Ok(await ReloadDto(report.Id, ct));
    }

    public async Task<Result<ExpenseReportDto>> UpdateAsync(UpdateExpenseReportCommand cmd, CancellationToken ct = default)
    {
        var report = await db.ExpenseReports
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == cmd.Id && e.TenantId == cmd.TenantId, ct);

        if (report is null)
            return Result<ExpenseReportDto>.Fail("Expense report not found.");
        if (report.Status is not (ExpenseReportStatus.Draft or ExpenseReportStatus.Rejected))
            return Result<ExpenseReportDto>.Fail("Only draft or rejected expense reports can be edited.");
        if (!cmd.Lines.Any())
            return Result<ExpenseReportDto>.Fail("An expense report must have at least one line.");

        report.Title = cmd.Title;
        report.SubmissionDate = cmd.SubmissionDate;
        report.Currency = cmd.Currency;
        report.UpdatedAt = DateTimeOffset.UtcNow;

        if (report.Status == ExpenseReportStatus.Rejected)
            report.RejectionReason = null;

        db.ExpenseReportLines.RemoveRange(report.Lines);
        ApplyLines(report, cmd.Lines);

        if (report.Status == ExpenseReportStatus.Rejected)
            report.Status = ExpenseReportStatus.Draft;

        await db.SaveChangesAsync(ct);
        return Result<ExpenseReportDto>.Ok(await ReloadDto(report.Id, ct));
    }

    public async Task<Result<ExpenseReportDto>> SubmitAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var report = await db.ExpenseReports
            .Include(e => e.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);

        if (report is null)
            return Result<ExpenseReportDto>.Fail("Expense report not found.");
        if (report.Status != ExpenseReportStatus.Draft)
            return Result<ExpenseReportDto>.Fail("Only draft expense reports can be submitted.");
        if (!report.Lines.Any())
            return Result<ExpenseReportDto>.Fail("Cannot submit an expense report with no lines.");

        report.Status = ExpenseReportStatus.PendingApproval;
        report.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result<ExpenseReportDto>.Ok(ToDto(report));
    }

    public async Task<Result<ExpenseReportDto>> ApproveAsync(ApproveExpenseReportCommand cmd, CancellationToken ct = default)
    {
        var report = await db.ExpenseReports
            .Include(e => e.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstOrDefaultAsync(e => e.Id == cmd.Id && e.TenantId == cmd.TenantId, ct);

        if (report is null)
            return Result<ExpenseReportDto>.Fail("Expense report not found.");
        if (report.Status != ExpenseReportStatus.PendingApproval)
            return Result<ExpenseReportDto>.Fail("Only Pending Approval expense reports can be approved.");
        if (!report.Lines.Any())
            return Result<ExpenseReportDto>.Fail("Cannot approve an expense report with no lines.");

        var apAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Code == ApAccountCode, ct);
        if (apAccount is null)
            return Result<ExpenseReportDto>.Fail($"AP account ({ApAccountCode}) not found. Please set up the chart of accounts.");

        var now = DateTimeOffset.UtcNow;
        var refCode = $"EXP-{report.Id.ToString("N")[..8].ToUpper()}";

        var entry = new JournalEntry
        {
            TenantId = cmd.TenantId,
            Date = report.SubmissionDate,
            Reference = refCode,
            Description = $"Expense report – {report.Title} ({report.SubmittedByName})",
            CreatedByUserId = cmd.PostedByUserId,
            CreatedByName = cmd.PostedByName,
            IsLocked = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Cr AP (full amount)
        entry.Lines.Add(new JournalLine
        {
            AccountId = apAccount.Id,
            Debit = 0m,
            Credit = report.TotalAmount,
            Currency = report.Currency,
            ExchangeRate = 1m,
            Memo = refCode,
        });

        // Dr each expense account (grouped)
        var expenseGroups = report.Lines
            .GroupBy(l => l.ExpenseAccountId)
            .Select(g => new { AccountId = g.Key, Amount = g.Sum(l => l.Amount) });

        foreach (var group in expenseGroups)
        {
            entry.Lines.Add(new JournalLine
            {
                AccountId = group.AccountId,
                Debit = group.Amount,
                Credit = 0m,
                Currency = report.Currency,
                ExchangeRate = 1m,
                Memo = refCode,
            });
        }

        db.JournalEntries.Add(entry);

        report.Status = ExpenseReportStatus.Approved;
        report.JournalEntryId = entry.Id;
        report.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return Result<ExpenseReportDto>.Ok(ToDto(report));
    }

    public async Task<Result<ExpenseReportDto>> RejectAsync(RejectExpenseReportCommand cmd, CancellationToken ct = default)
    {
        var report = await db.ExpenseReports
            .Include(e => e.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstOrDefaultAsync(e => e.Id == cmd.Id && e.TenantId == cmd.TenantId, ct);

        if (report is null)
            return Result<ExpenseReportDto>.Fail("Expense report not found.");
        if (report.Status != ExpenseReportStatus.PendingApproval)
            return Result<ExpenseReportDto>.Fail("Only Pending Approval expense reports can be rejected.");

        report.Status = ExpenseReportStatus.Rejected;
        report.RejectionReason = cmd.RejectionReason;
        report.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<ExpenseReportDto>.Ok(ToDto(report));
    }

    public async Task<Result<ExpenseReportDto>> RecordPaymentAsync(RecordExpensePaymentCommand cmd, CancellationToken ct = default)
    {
        var report = await db.ExpenseReports
            .Include(e => e.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstOrDefaultAsync(e => e.Id == cmd.Id && e.TenantId == cmd.TenantId, ct);

        if (report is null)
            return Result<ExpenseReportDto>.Fail("Expense report not found.");
        if (report.Status != ExpenseReportStatus.Approved)
            return Result<ExpenseReportDto>.Fail("Only approved expense reports can be paid.");

        var apAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Code == ApAccountCode, ct);
        if (apAccount is null)
            return Result<ExpenseReportDto>.Fail($"AP account ({ApAccountCode}) not found.");

        var bankAccount = await db.Accounts.FindAsync([cmd.BankAccountId], ct);
        if (bankAccount is null)
            return Result<ExpenseReportDto>.Fail("Bank account not found.");

        var now = DateTimeOffset.UtcNow;
        var refCode = $"PMT-EXP-{report.Id.ToString("N")[..8].ToUpper()}";

        var entry = new JournalEntry
        {
            TenantId = cmd.TenantId,
            Date = cmd.Date,
            Reference = refCode,
            Description = $"Payment of expense report – {report.Title}",
            CreatedByUserId = cmd.PostedByUserId,
            CreatedByName = cmd.PostedByName,
            IsLocked = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Dr AP
        entry.Lines.Add(new JournalLine
        {
            AccountId = apAccount.Id,
            Debit = report.TotalAmount,
            Credit = 0m,
            Currency = report.Currency,
            ExchangeRate = 1m,
            Memo = refCode,
        });

        // Cr Bank
        entry.Lines.Add(new JournalLine
        {
            AccountId = bankAccount.Id,
            Debit = 0m,
            Credit = report.TotalAmount,
            Currency = report.Currency,
            ExchangeRate = 1m,
            Memo = refCode,
        });

        db.JournalEntries.Add(entry);

        report.Status = ExpenseReportStatus.Paid;
        report.PaymentJournalEntryId = entry.Id;
        report.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return Result<ExpenseReportDto>.Ok(ToDto(report));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var report = await db.ExpenseReports
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);

        if (report is null)
            return Result.Fail("Expense report not found.");
        if (report.Status is not (ExpenseReportStatus.Draft or ExpenseReportStatus.Rejected))
            return Result.Fail("Only draft or rejected expense reports can be deleted.");

        db.ExpenseReports.Remove(report);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public Task<int> GetPendingApprovalCountAsync(Guid tenantId, CancellationToken ct = default)
        => db.ExpenseReports.CountAsync(e => e.TenantId == tenantId && e.Status == ExpenseReportStatus.PendingApproval, ct);

    public async Task<(byte[] Data, string ContentType, string FileName)?> DownloadReceiptAsync(Guid lineId, Guid tenantId, CancellationToken ct = default)
    {
        var line = await db.ExpenseReportLines
            .Include(l => l.ExpenseReport)
            .FirstOrDefaultAsync(l => l.Id == lineId && l.ExpenseReport.TenantId == tenantId, ct);

        if (line?.ReceiptData is null || line.ReceiptFileName is null)
            return null;

        return (line.ReceiptData, line.ReceiptContentType ?? "application/octet-stream", line.ReceiptFileName);
    }

    private static void ApplyLines(ExpenseReport report, IReadOnlyList<ExpenseReportLineInput> inputs)
    {
        foreach (var (l, i) in inputs.Select((l, i) => (l, i)))
        {
            var line = new ExpenseReportLine
            {
                ExpenseReportId = report.Id,
                Description = l.Description,
                Date = l.Date,
                Amount = l.Amount,
                Currency = l.Currency,
                ExpenseAccountId = l.ExpenseAccountId,
                SortOrder = l.SortOrder == 0 ? i : l.SortOrder,
            };

            if (l.ReceiptData is { Length: > 0 } && l.ReceiptFileName is not null)
            {
                line.ReceiptData = l.ReceiptData;
                line.ReceiptFileName = l.ReceiptFileName;
                line.ReceiptContentType = l.ReceiptContentType ?? "application/octet-stream";
            }

            report.Lines.Add(line);
        }

        report.TotalAmount = report.Lines.Sum(l => l.Amount);
    }

    private async Task<ExpenseReportDto> ReloadDto(Guid id, CancellationToken ct)
    {
        var report = await db.ExpenseReports
            .Include(e => e.Lines).ThenInclude(l => l.ExpenseAccount)
            .FirstAsync(e => e.Id == id, ct);
        return ToDto(report);
    }

    private static ExpenseReportDto ToDto(ExpenseReport r) => new(
        r.Id,
        r.TenantId,
        r.Title,
        r.SubmissionDate,
        r.SubmittedByUserId,
        r.SubmittedByName,
        r.Status,
        r.Currency,
        r.TotalAmount,
        r.Lines.OrderBy(l => l.SortOrder).Select(l => new ExpenseReportLineDto(
            l.Id,
            l.Description,
            l.Date,
            l.Amount,
            l.Currency,
            l.ExpenseAccountId,
            l.ExpenseAccount?.Name ?? string.Empty,
            l.ReceiptData is { Length: > 0 },
            l.SortOrder)).ToList(),
        r.RejectionReason);
}
