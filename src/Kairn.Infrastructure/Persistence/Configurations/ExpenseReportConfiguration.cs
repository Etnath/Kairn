using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class ExpenseReportConfiguration : IEntityTypeConfiguration<ExpenseReport>
{
    public void Configure(EntityTypeBuilder<ExpenseReport> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.SubmittedByUserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.SubmittedByName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Currency).IsRequired().HasMaxLength(3);
        builder.Property(e => e.RejectionReason).HasMaxLength(2000);

        builder.HasMany(e => e.Lines)
            .WithOne(l => l.ExpenseReport)
            .HasForeignKey(l => l.ExpenseReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.SubmissionDate });
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}

public class ExpenseReportLineConfiguration : IEntityTypeConfiguration<ExpenseReportLine>
{
    public void Configure(EntityTypeBuilder<ExpenseReportLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);
        builder.Property(l => l.Currency).IsRequired().HasMaxLength(3);
        builder.Property(l => l.ReceiptFileName).HasMaxLength(255);
        builder.Property(l => l.ReceiptContentType).HasMaxLength(100);

        builder.HasOne(l => l.ExpenseAccount)
            .WithMany()
            .HasForeignKey(l => l.ExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.ExpenseReportId);
        builder.HasIndex(l => l.ExpenseAccountId);
    }
}
