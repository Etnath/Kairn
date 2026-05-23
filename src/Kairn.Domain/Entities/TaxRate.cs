using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum TaxCategory { Standard, Intermediate, Reduced, SuperReduced, Exempt }

public class TaxRate : BaseEntity
{
    public string      Name      { get; set; } = "";
    public decimal     Rate      { get; set; }
    public TaxCategory Category  { get; set; }
    public bool        IsDefault { get; set; }
    public DateOnly    ValidFrom { get; set; }
    public DateOnly?   ValidTo   { get; set; }
    public bool        IsActive  { get; set; } = true;
}
