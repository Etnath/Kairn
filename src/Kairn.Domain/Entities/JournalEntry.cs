using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public class JournalEntry : BaseEntity
{
    public DateOnly Date { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? RecurringEntryId { get; set; }
    public string? AttachmentPath { get; set; }
    public string? AttachmentFileName { get; set; }

    public ICollection<JournalLine> Lines { get; set; } = [];

    public decimal TotalDebit => Lines.Sum(l => l.Debit);
    public decimal TotalCredit => Lines.Sum(l => l.Credit);
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.0001m;
}
