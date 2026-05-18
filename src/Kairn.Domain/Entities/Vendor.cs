using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public class Vendor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? IBAN { get; set; }
    public int PaymentTermsDays { get; set; } = 30;
    public Guid? DefaultExpenseAccountId { get; set; }
    public bool IsActive { get; set; } = true;

    public Account? DefaultExpenseAccount { get; set; }
}
