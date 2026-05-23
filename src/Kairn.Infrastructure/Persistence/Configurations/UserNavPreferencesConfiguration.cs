using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class UserNavPreferencesConfiguration : IEntityTypeConfiguration<UserNavPreferences>
{
    public void Configure(EntityTypeBuilder<UserNavPreferences> b)
    {
        b.HasKey(x => x.UserId);
        b.Property(x => x.UserId).HasMaxLength(450);
        b.Property(x => x.CollapsedGroups).HasMaxLength(500);
        b.ToTable("UserNavPreferences");
    }
}
