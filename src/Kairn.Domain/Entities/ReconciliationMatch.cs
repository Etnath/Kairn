namespace Kairn.Domain.Entities;

public class ReconciliationMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid BankLineId { get; set; }
    public Guid JournalLineId { get; set; }
    public DateTimeOffset MatchedAt { get; set; }
    public string MatchedByUserId { get; set; } = string.Empty;

    public BankStatementLine BankLine { get; set; } = null!;
    public JournalLine JournalLine { get; set; } = null!;
}
