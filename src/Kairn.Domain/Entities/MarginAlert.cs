using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public class MarginAlert : BaseEntity
{
    public Guid     ProductLineId     { get; set; }
    public string   ProductLineName   { get; set; } = "";
    public DateOnly Month             { get; set; }
    public decimal  MarginPct         { get; set; }
    public decimal  ThresholdPct      { get; set; }
    public bool     IsDismissed       { get; set; }
    public DateTimeOffset? DismissedAt       { get; set; }
    public string?         DismissedByUserId { get; set; }
}
