using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public class BankStatementLine : BaseEntity
{
    public Guid SessionId { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? ExternalId { get; set; }
    public bool IsMatched { get; set; }

    public ReconciliationSession Session { get; set; } = null!;
    public ICollection<ReconciliationMatch> Matches { get; set; } = [];
}
