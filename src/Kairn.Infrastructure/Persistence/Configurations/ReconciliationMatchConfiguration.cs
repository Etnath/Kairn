using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class ReconciliationMatchConfiguration : IEntityTypeConfiguration<ReconciliationMatch>
{
    public void Configure(EntityTypeBuilder<ReconciliationMatch> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.MatchedByUserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(m => new { m.SessionId, m.BankLineId });
        builder.HasIndex(m => m.JournalLineId);

        builder.HasOne(m => m.JournalLine)
               .WithMany()
               .HasForeignKey(m => m.JournalLineId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
