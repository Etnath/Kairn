namespace Kairn.Domain.Entities;

public class CurrencyRate
{
    public long Id { get; set; }
    /// <summary>Foreign/Base, e.g. "EUR/CHF" means "how many CHF per 1 EUR"</summary>
    public string CurrencyPair { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal Rate { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
}
