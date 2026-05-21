using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum ExpenseReportStatus { Draft, PendingApproval, Approved, Paid, Rejected }

public class ExpenseReport : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public DateOnly SubmissionDate { get; set; }
    public string SubmittedByUserId { get; set; } = string.Empty;
    public string SubmittedByName { get; set; } = string.Empty;
    public ExpenseReportStatus Status { get; set; } = ExpenseReportStatus.Draft;
    public string Currency { get; set; } = "EUR";
    public decimal TotalAmount { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid? PaymentJournalEntryId { get; set; }
    public string? RejectionReason { get; set; }

    public ICollection<ExpenseReportLine> Lines { get; set; } = [];
}

public class ExpenseReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExpenseReportId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public Guid ExpenseAccountId { get; set; }
    public string? ReceiptFileName { get; set; }
    public string? ReceiptContentType { get; set; }
    public byte[]? ReceiptData { get; set; }
    public int SortOrder { get; set; }

    public ExpenseReport ExpenseReport { get; set; } = null!;
    public Account ExpenseAccount { get; set; } = null!;
}
