using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class RecurringJobLogConfiguration : IEntityTypeConfiguration<RecurringJobLog>
{
    public void Configure(EntityTypeBuilder<RecurringJobLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();
        builder.Property(l => l.EntryName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.ErrorMessage).HasMaxLength(2000);
        builder.Property(l => l.PostedReference).HasMaxLength(30);

        builder.HasIndex(l => new { l.TenantId, l.AttemptedAt });
    }
}
