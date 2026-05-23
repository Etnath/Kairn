using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public class TaxPeriod : BaseEntity
{
    public string    Name              { get; set; } = "";
    public DateOnly  StartDate         { get; set; }
    public DateOnly  EndDate           { get; set; }
    public bool      IsLocked          { get; set; }
    public string?   LockedByUserId    { get; set; }
    public string?   LockedByUserName  { get; set; }
    public DateTimeOffset? LockedAt   { get; set; }
}
