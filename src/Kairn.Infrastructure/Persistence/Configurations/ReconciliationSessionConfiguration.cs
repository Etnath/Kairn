using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class ReconciliationSessionConfiguration : IEntityTypeConfiguration<ReconciliationSession>
{
    public void Configure(EntityTypeBuilder<ReconciliationSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.StatementFileName).HasMaxLength(260);
        builder.HasIndex(s => new { s.TenantId, s.AccountId, s.StartDate, s.EndDate });

        builder.HasOne(s => s.Account)
               .WithMany()
               .HasForeignKey(s => s.AccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.BankLines)
               .WithOne(l => l.Session)
               .HasForeignKey(l => l.SessionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
