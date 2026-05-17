using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class RecurringEntryConfiguration : IEntityTypeConfiguration<RecurringEntry>
{
    public void Configure(EntityTypeBuilder<RecurringEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.EntryDescription).IsRequired().HasMaxLength(500);

        builder.HasIndex(e => new { e.TenantId, e.IsActive, e.NextDueDate });

        builder.HasMany(e => e.Lines)
               .WithOne(l => l.RecurringEntry)
               .HasForeignKey(l => l.RecurringEntryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
