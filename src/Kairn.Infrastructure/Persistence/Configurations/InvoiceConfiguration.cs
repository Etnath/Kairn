using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Reference).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.Notes).HasMaxLength(2000);
        builder.Property(i => i.Status).HasConversion<string>();

        builder.HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Lines)
            .WithOne(l => l.Invoice)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.CreditNotes)
            .WithOne(cn => cn.OriginalInvoice)
            .HasForeignKey(cn => cn.OriginalInvoiceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.TenantId, i.Date });
        builder.HasIndex(i => new { i.TenantId, i.Status });
        builder.HasIndex(i => i.Reference).IsUnique();
    }
}

public class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Reference).HasMaxLength(100);
        builder.Property(p => p.Method).HasConversion<string>();

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.InvoiceId);
    }
}

public class InvoiceReminderConfiguration : IEntityTypeConfiguration<InvoiceReminder>
{
    public void Configure(EntityTypeBuilder<InvoiceReminder> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SentByUserId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.SentByName).IsRequired().HasMaxLength(256);
        builder.Property(r => r.Method).IsRequired().HasMaxLength(20);

        builder.HasOne(r => r.Invoice)
            .WithMany(i => i.Reminders)
            .HasForeignKey(r => r.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.InvoiceId);
    }
}

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);
        builder.Ignore(l => l.GrossAmount);
        builder.Ignore(l => l.DiscountAmount);
        builder.Ignore(l => l.NetAmount);
        builder.Ignore(l => l.TaxAmount);
        builder.Ignore(l => l.LineTotal);
    }
}
