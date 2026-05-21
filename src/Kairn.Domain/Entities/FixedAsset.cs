using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum DepreciationMethod { StraightLine, DecliningBalance }

public class FixedAsset : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal PurchaseValue { get; set; }
    public decimal ResidualValue { get; set; }
    public DepreciationMethod Method { get; set; }
    public int UsefulLifeYears { get; set; }
    public Guid AssetAccountId { get; set; }
    public Guid AccumulatedDepreciationAccountId { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public DateOnly? LastDepreciatedDate { get; set; }
    public Guid? DepreciationExpenseAccountId { get; set; }
    public bool HasDepreciationPostings { get; set; }
    public bool IsFullyDepreciated { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? DisposalDate { get; set; }
    public Guid? DisposalJournalEntryId { get; set; }

    public Account AssetAccount { get; set; } = null!;
    public Account AccumulatedDepreciationAccount { get; set; } = null!;
    public Account? DepreciationExpenseAccount { get; set; }

    public decimal NetBookValue => PurchaseValue - AccumulatedDepreciation;

    public DateOnly? NextDepreciationDate
    {
        get
        {
            if (!IsActive || NetBookValue <= ResidualValue) return null;
            if (LastDepreciatedDate is null)
            {
                var y = PurchaseDate.Year;
                var m = PurchaseDate.Month;
                return new DateOnly(y, m, DateTime.DaysInMonth(y, m));
            }
            var next = LastDepreciatedDate.Value.AddMonths(1);
            return new DateOnly(next.Year, next.Month, DateTime.DaysInMonth(next.Year, next.Month));
        }
    }
}
