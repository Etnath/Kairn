using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum ReconciliationStatus { InProgress, Completed }
public enum StatementFormat { OFX, QFX, CSV }

public class ReconciliationSession : BaseEntity
{
    public Guid AccountId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ReconciliationStatus Status { get; set; } = ReconciliationStatus.InProgress;
    public DateTimeOffset? CompletedAt { get; set; }
    public int MatchedPairCount { get; set; }
    public string? StatementFileName { get; set; }
    public StatementFormat? Format { get; set; }

    public Account Account { get; set; } = null!;
    public ICollection<BankStatementLine> BankLines { get; set; } = [];
}
