using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public class FiscalYearClose : BaseEntity
{
    public int FiscalYear { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string ClosedByUserId { get; set; } = string.Empty;
    public string ClosedByName { get; set; } = string.Empty;

    public JournalEntry? JournalEntry { get; set; }
}
