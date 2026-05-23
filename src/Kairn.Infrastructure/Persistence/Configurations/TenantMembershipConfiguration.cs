using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> b)
    {
        b.HasKey(m => new { m.TenantId, m.UserId });
        b.Property(m => m.UserId).HasMaxLength(450);
        b.Property(m => m.Role).HasConversion<string>();
        b.HasOne(m => m.Tenant)
         .WithMany()
         .HasForeignKey(m => m.TenantId)
         .OnDelete(DeleteBehavior.Cascade);
        b.ToTable("TenantMemberships");
    }
}
