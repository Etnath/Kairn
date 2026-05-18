using Kairn.Application.Common;
using Kairn.Application.Features.AR;
using Kairn.Domain.Entities;
using Kairn.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class ReminderService(AppDbContext db, IEmailService emailSvc, IInvoiceService invoiceSvc) : IReminderService
{
    public async Task<IReadOnlyList<InvoiceReminderDto>> GetByInvoiceAsync(
        Guid invoiceId, Guid tenantId, CancellationToken ct = default)
    {
        var reminders = await db.InvoiceReminders
            .Where(r => r.InvoiceId == invoiceId && r.TenantId == tenantId)
            .OrderBy(r => r.SentDate)
            .ToListAsync(ct);
        return reminders.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<BulkReminderRecipientDto>> GetBulkPreviewAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var invoices = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.TenantId == tenantId && i.Status == InvoiceStatus.Overdue)
            .ToListAsync(ct);

        return invoices
            .GroupBy(i => new { i.CustomerId, i.Customer.Name, i.Customer.Email })
            .Select(g =>
            {
                var outstanding = g.Sum(i =>
                    i.Lines.Sum(l => l.LineTotal) - i.Payments.Sum(p => p.Amount));
                return new BulkReminderRecipientDto(
                    g.Key.CustomerId,
                    g.Key.Name,
                    g.Key.Email,
                    g.Count(),
                    outstanding);
            })
            .OrderByDescending(r => r.TotalOutstanding)
            .ToList();
    }

    public async Task<Result<InvoiceReminderDto>> SendAsync(
        SendReminderCommand cmd, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == cmd.InvoiceId && i.TenantId == cmd.TenantId, ct);

        if (invoice is null)
            return Result<InvoiceReminderDto>.Fail("Invoice not found.");
        if (invoice.Status != InvoiceStatus.Overdue)
            return Result<InvoiceReminderDto>.Fail("Reminders can only be sent for overdue invoices.");

        var outstanding = invoice.Lines.Sum(l => l.LineTotal) - invoice.Payments.Sum(p => p.Amount);
        var method = "Print";

        if (cmd.SendEmail)
        {
            if (string.IsNullOrWhiteSpace(invoice.Customer.Email))
                return Result<InvoiceReminderDto>.Fail("Customer has no email address on record.");
            if (!emailSvc.IsConfigured)
                return Result<InvoiceReminderDto>.Fail("SMTP is not configured. Please set up email settings.");

            var html = ReminderLetterGenerator.GenerateHtml(
                invoice.Customer.Name, invoice.Customer.Address,
                invoice.Reference, invoice.DueDate,
                invoice.Currency, outstanding,
                cmd.MessageText, cmd.SentByName);

            var pdfBytes = await invoiceSvc.GeneratePdfAsync(invoice.Id, cmd.TenantId, ct);

            await emailSvc.SendAsync(
                invoice.Customer.Email,
                $"Relance de paiement – Facture {invoice.Reference}",
                html,
                pdfBytes,
                $"{invoice.Reference}.pdf",
                ct);

            method = "Email";
        }

        var now = DateTimeOffset.UtcNow;
        var reminder = new InvoiceReminder
        {
            TenantId = cmd.TenantId,
            InvoiceId = cmd.InvoiceId,
            SentDate = DateOnly.FromDateTime(now.UtcDateTime),
            SentByUserId = cmd.SentByUserId,
            SentByName = cmd.SentByName,
            Method = method,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.InvoiceReminders.Add(reminder);
        await db.SaveChangesAsync(ct);
        return Result<InvoiceReminderDto>.Ok(ToDto(reminder));
    }

    public async Task<BulkReminderResultDto> SendBulkAsync(
        SendBulkReminderCommand cmd, CancellationToken ct = default)
    {
        var invoices = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.TenantId == cmd.TenantId && i.Status == InvoiceStatus.Overdue)
            .ToListAsync(ct);

        var groups = invoices.GroupBy(i => i.Customer);
        int sent = 0, skipped = 0;
        var errors = new List<string>();
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        foreach (var group in groups)
        {
            var customer = group.Key;
            var method = "Bulk";

            if (emailSvc.IsConfigured && !string.IsNullOrWhiteSpace(customer.Email))
            {
                try
                {
                    var lines = group.Select(inv =>
                    {
                        var outstanding = inv.Lines.Sum(l => l.LineTotal) - inv.Payments.Sum(p => p.Amount);
                        return (inv.Reference, inv.DueDate, inv.Currency, outstanding);
                    }).ToList();

                    var html = ReminderLetterGenerator.GenerateBulkHtml(
                        customer.Name, customer.Address,
                        lines, cmd.SentByName);

                    await emailSvc.SendAsync(
                        customer.Email,
                        $"Relance de paiement – {group.Count()} facture(s) en retard",
                        html, ct: ct);

                    method = "Email";
                    sent++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{customer.Name}: {ex.Message}");
                    skipped++;
                    continue;
                }
            }
            else
            {
                skipped++;
            }

            foreach (var inv in group)
            {
                db.InvoiceReminders.Add(new InvoiceReminder
                {
                    TenantId = cmd.TenantId,
                    InvoiceId = inv.Id,
                    SentDate = today,
                    SentByUserId = cmd.SentByUserId,
                    SentByName = cmd.SentByName,
                    Method = method,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return new BulkReminderResultDto(sent, skipped, errors);
    }

    private static InvoiceReminderDto ToDto(InvoiceReminder r) =>
        new(r.Id, r.SentDate, r.SentByName, r.Method, r.CreatedAt);
}
