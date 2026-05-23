using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class TaxPeriodConfiguration : IEntityTypeConfiguration<TaxPeriod>
{
    public void Configure(EntityTypeBuilder<TaxPeriod> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.LockedByUserId).HasMaxLength(450);
        builder.Property(t => t.LockedByUserName).HasMaxLength(256);
        builder.HasIndex(t => new { t.TenantId, t.StartDate });
        builder.ToTable("TaxPeriods");
    }
}
