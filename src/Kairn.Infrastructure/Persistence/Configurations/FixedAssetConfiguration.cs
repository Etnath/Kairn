using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Category).HasMaxLength(100);
        b.Property(x => x.PurchaseValue).HasPrecision(18, 4);
        b.Property(x => x.ResidualValue).HasPrecision(18, 4);
        b.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 4);
        b.HasIndex(x => x.TenantId);
        b.Ignore(x => x.NetBookValue);
        b.Ignore(x => x.NextDepreciationDate);

        b.HasOne(x => x.AssetAccount)
            .WithMany()
            .HasForeignKey(x => x.AssetAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.AccumulatedDepreciationAccount)
            .WithMany()
            .HasForeignKey(x => x.AccumulatedDepreciationAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(x => x.DisposalJournalEntryId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        b.HasOne(x => x.DepreciationExpenseAccount)
            .WithMany()
            .HasForeignKey(x => x.DepreciationExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
