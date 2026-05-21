using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public class Budget : BaseEntity
{
    public int FiscalYear { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<BudgetLine> Lines { get; set; } = [];
}

public class BudgetLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetId { get; set; }
    public Guid AccountId { get; set; }
    public int Month { get; set; }  // 1–12
    public decimal Amount { get; set; }

    public Budget Budget { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
