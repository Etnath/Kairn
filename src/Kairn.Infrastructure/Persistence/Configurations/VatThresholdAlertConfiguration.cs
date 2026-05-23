using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class VatThresholdAlertConfiguration : IEntityTypeConfiguration<VatThresholdAlert>
{
    public void Configure(EntityTypeBuilder<VatThresholdAlert> b)
    {
        b.Property(x => x.Level).HasConversion<string>();
        // At most one alert per tenant/year/level (de-duplication per spec)
        b.HasIndex(x => new { x.TenantId, x.Year, x.Level }).IsUnique();
    }
}
