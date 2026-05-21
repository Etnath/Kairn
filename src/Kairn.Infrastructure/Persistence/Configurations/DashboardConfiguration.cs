using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class TenantDashboardSettingsConfiguration : IEntityTypeConfiguration<TenantDashboardSettings>
{
    public void Configure(EntityTypeBuilder<TenantDashboardSettings> builder)
    {
        builder.HasKey(s => s.TenantId);
        builder.Property(s => s.CashAlertThreshold).HasPrecision(18, 4);
        builder.ToTable("TenantDashboardSettings");
    }
}
