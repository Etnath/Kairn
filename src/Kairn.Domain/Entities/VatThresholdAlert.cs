using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum VatThresholdLevel { Warning, Exceeded }

public class VatThresholdAlert : BaseEntity
{
    public int                Year                { get; set; }
    public VatThresholdLevel  Level               { get; set; }
    public decimal            YtdRevenue          { get; set; }
    public decimal            Threshold           { get; set; }
    public bool               IsDismissed         { get; set; }
    public DateTimeOffset?    DismissedAt         { get; set; }
    public string?            DismissedByUserId   { get; set; }
}
