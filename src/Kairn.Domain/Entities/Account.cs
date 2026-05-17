using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum AccountType { Asset, Liability, Equity, Revenue, Expense }

public class Account : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public Guid? ParentId { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsActive { get; set; } = true;
    public bool IsCurrent { get; set; } = true;
}
