namespace Kairn.Domain.Entities;

public class InvoiceLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPct { get; set; }   // 0–100
    public decimal TaxRate    { get; set; }       // 0–100
    public Guid?   TaxRateId  { get; set; }
    public int     SortOrder  { get; set; }

    public Invoice Invoice { get; set; } = null!;

    public decimal GrossAmount => Quantity * UnitPrice;
    public decimal DiscountAmount => GrossAmount * DiscountPct / 100m;
    public decimal NetAmount => GrossAmount - DiscountAmount;
    public decimal TaxAmount => NetAmount * TaxRate / 100m;
    public decimal LineTotal => NetAmount + TaxAmount;
}
