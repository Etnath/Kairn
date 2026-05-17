namespace Kairn.Domain.Entities;

public class RecurringEntryLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecurringEntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal ExchangeRate { get; set; } = 1m;
    public string? Memo { get; set; }

    public RecurringEntry RecurringEntry { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
