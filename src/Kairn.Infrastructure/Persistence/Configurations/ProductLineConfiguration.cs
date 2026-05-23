using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class ProductLineConfiguration : IEntityTypeConfiguration<ProductLine>
{
    public void Configure(EntityTypeBuilder<ProductLine> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.TenantId, p.Name }).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.MarginAlertThreshold).HasPrecision(18, 4);
        builder.Property(p => p.OpExAllocationPct).HasPrecision(7, 4);
        builder.HasMany(p => p.Accounts)
               .WithOne()
               .HasForeignKey(a => a.ProductLineId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("ProductLines");
    }
}

public class ProductLineAccountConfiguration : IEntityTypeConfiguration<ProductLineAccount>
{
    public void Configure(EntityTypeBuilder<ProductLineAccount> builder)
    {
        builder.HasKey(a => new { a.ProductLineId, a.AccountId, a.Role });
        builder.Property(a => a.Role).HasConversion<string>().HasMaxLength(20);
        builder.ToTable("ProductLineAccounts");
    }
}
