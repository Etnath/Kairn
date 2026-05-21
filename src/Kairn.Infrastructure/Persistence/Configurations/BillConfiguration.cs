using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Reference).IsRequired().HasMaxLength(100);
        builder.Property(b => b.Currency).IsRequired().HasMaxLength(3);
        builder.Property(b => b.Notes).HasMaxLength(2000);

        builder.HasOne(b => b.Vendor)
            .WithMany()
            .HasForeignKey(b => b.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Lines)
            .WithOne(l => l.Bill)
            .HasForeignKey(l => l.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Attachments)
            .WithOne(a => a.Bill)
            .HasForeignKey(a => a.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.TenantId, b.Date });
        builder.HasIndex(b => new { b.TenantId, b.VendorId });
    }
}

public class BillLineConfiguration : IEntityTypeConfiguration<BillLine>
{
    public void Configure(EntityTypeBuilder<BillLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);

        builder.HasOne(l => l.ExpenseAccount)
            .WithMany()
            .HasForeignKey(l => l.ExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillAttachmentConfiguration : IEntityTypeConfiguration<BillAttachment>
{
    public void Configure(EntityTypeBuilder<BillAttachment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
    }
}

public class TenantApSettingsConfiguration : IEntityTypeConfiguration<TenantApSettings>
{
    public void Configure(EntityTypeBuilder<TenantApSettings> builder)
    {
        builder.HasKey(s => s.TenantId);
        builder.Property(s => s.ApproverRoles).IsRequired().HasMaxLength(200);
        builder.Property(s => s.ApprovalThreshold).HasPrecision(18, 4);
        builder.ToTable("TenantApSettings");
    }
}

public class BillPaymentConfiguration : IEntityTypeConfiguration<BillPayment>
{
    public void Configure(EntityTypeBuilder<BillPayment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Reference).HasMaxLength(100);
        builder.Property(p => p.Method).HasConversion<string>();

        builder.HasOne(p => p.Bill)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.BillId);
        builder.HasIndex(p => new { p.TenantId, p.Date });
    }
}
