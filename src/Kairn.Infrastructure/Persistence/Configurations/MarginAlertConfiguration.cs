using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class MarginAlertConfiguration : IEntityTypeConfiguration<MarginAlert>
{
    public void Configure(EntityTypeBuilder<MarginAlert> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.TenantId, a.ProductLineId, a.Month }).IsUnique();
        builder.HasIndex(a => new { a.TenantId, a.IsDismissed });
        builder.Property(a => a.ProductLineName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.MarginPct).HasPrecision(18, 4);
        builder.Property(a => a.ThresholdPct).HasPrecision(18, 4);
        builder.Property(a => a.DismissedByUserId).HasMaxLength(450);
        builder.ToTable("MarginAlerts");
    }
}
