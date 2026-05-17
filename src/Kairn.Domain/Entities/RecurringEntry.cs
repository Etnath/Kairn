using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum RecurringFrequency { Daily, Weekly, Monthly, Quarterly, Annually }

public class RecurringEntry : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string EntryDescription { get; set; } = string.Empty;
    public RecurringFrequency Frequency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? LastPostedDate { get; set; }
    public DateOnly NextDueDate { get; set; }

    public ICollection<RecurringEntryLine> Lines { get; set; } = [];
}
