using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.HasIndex(x => new { x.TenantId, x.FiscalYear });
        b.HasMany(x => x.Lines)
            .WithOne(l => l.Budget)
            .HasForeignKey(l => l.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BudgetLineConfiguration : IEntityTypeConfiguration<BudgetLine>
{
    public void Configure(EntityTypeBuilder<BudgetLine> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.HasOne(x => x.Budget)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.BudgetId);
        b.HasIndex(x => new { x.BudgetId, x.AccountId, x.Month }).IsUnique();
    }
}
