using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class UserDashboardPreferencesConfiguration : IEntityTypeConfiguration<UserDashboardPreferences>
{
    public void Configure(EntityTypeBuilder<UserDashboardPreferences> builder)
    {
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId).HasMaxLength(450);
        builder.ToTable("UserDashboardPreferences");
    }
}
