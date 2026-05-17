using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Currency).IsRequired().HasMaxLength(3);
        builder.Property(l => l.Memo).HasMaxLength(500);

        builder.HasIndex(l => new { l.AccountId, l.IsReconciled });

        builder.HasOne(l => l.Account)
               .WithMany()
               .HasForeignKey(l => l.AccountId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
