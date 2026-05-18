using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public int PaymentTermsDays { get; set; } = 30;
    public decimal? CreditLimit { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsActive { get; set; } = true;
}
