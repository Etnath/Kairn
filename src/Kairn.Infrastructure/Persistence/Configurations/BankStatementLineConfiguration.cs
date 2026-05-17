using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class BankStatementLineConfiguration : IEntityTypeConfiguration<BankStatementLine>
{
    public void Configure(EntityTypeBuilder<BankStatementLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);
        builder.Property(l => l.Currency).IsRequired().HasMaxLength(3);
        builder.Property(l => l.ExternalId).HasMaxLength(100);

        builder.HasMany(l => l.Matches)
               .WithOne(m => m.BankLine)
               .HasForeignKey(m => m.BankLineId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
