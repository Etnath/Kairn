using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum ProductLineAccountRole { Revenue, Cogs }

public class ProductLine : BaseEntity
{
    public string   Name                 { get; set; } = "";
    public string?  Description          { get; set; }
    public decimal? MarginAlertThreshold { get; set; }
    public decimal? OpExAllocationPct   { get; set; }
    public bool     IsActive             { get; set; } = true;

    public ICollection<ProductLineAccount> Accounts { get; set; } = new List<ProductLineAccount>();
}

public class ProductLineAccount
{
    public Guid                   ProductLineId { get; set; }
    public Guid                   AccountId     { get; set; }
    public ProductLineAccountRole Role          { get; set; }
}
