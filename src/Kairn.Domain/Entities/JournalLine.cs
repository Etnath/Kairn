namespace Kairn.Domain.Entities;

public class JournalLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal ExchangeRate { get; set; } = 1m;
    public string? Memo { get; set; }

    public JournalEntry Entry { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
