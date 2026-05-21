using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class FiscalYearCloseConfiguration : IEntityTypeConfiguration<FiscalYearClose>
{
    public void Configure(EntityTypeBuilder<FiscalYearClose> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.ClosedByUserId).IsRequired().HasMaxLength(450);
        b.Property(x => x.ClosedByName).IsRequired().HasMaxLength(200);

        b.HasIndex(x => new { x.TenantId, x.FiscalYear }).IsUnique();

        b.HasOne(x => x.JournalEntry)
            .WithMany()
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
