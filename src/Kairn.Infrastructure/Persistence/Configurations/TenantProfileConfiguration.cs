using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class TenantProfileConfiguration : IEntityTypeConfiguration<TenantProfile>
{
    public void Configure(EntityTypeBuilder<TenantProfile> builder)
    {
        builder.HasKey(p => p.TenantId);
        builder.Property(p => p.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Siret).HasMaxLength(14);
        builder.Property(p => p.AddressLine).HasMaxLength(200);
        builder.Property(p => p.PostalCode).HasMaxLength(20);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.Country).HasMaxLength(100);
        builder.Property(p => p.LogoPath).HasMaxLength(500);
        builder.Property(p => p.BusinessStatus).HasConversion<string>();
        builder.Property(p => p.ActivityType).HasConversion<string>();
        builder.ToTable("TenantProfiles");
    }
}
