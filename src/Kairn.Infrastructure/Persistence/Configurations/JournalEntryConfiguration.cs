using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Reference).IsRequired().HasMaxLength(30);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
        builder.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.CreatedByName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.AttachmentPath).HasMaxLength(1000);
        builder.Property(e => e.AttachmentFileName).HasMaxLength(260);

        builder.HasIndex(e => new { e.TenantId, e.Reference }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Date });

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Ignore(e => e.TotalDebit);
        builder.Ignore(e => e.TotalCredit);
        builder.Ignore(e => e.IsBalanced);

        builder.HasOne<Kairn.Domain.Entities.RecurringEntry>()
               .WithMany()
               .HasForeignKey(e => e.RecurringEntryId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasMany(e => e.Lines)
               .WithOne(l => l.Entry)
               .HasForeignKey(l => l.EntryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
