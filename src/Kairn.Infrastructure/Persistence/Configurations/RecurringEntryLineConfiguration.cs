using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class RecurringEntryLineConfiguration : IEntityTypeConfiguration<RecurringEntryLine>
{
    public void Configure(EntityTypeBuilder<RecurringEntryLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Currency).IsRequired().HasMaxLength(3);
        builder.Property(l => l.Memo).HasMaxLength(500);

        builder.HasOne(l => l.Account)
               .WithMany()
               .HasForeignKey(l => l.AccountId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
