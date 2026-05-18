using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum InvoiceStatus { Draft, Sent, PartiallyPaid, Paid, Overdue, Void }

public class Invoice : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string Currency { get; set; } = "EUR";
    public string? Notes { get; set; }
    public Guid? RevenueAccountId { get; set; }
    public Guid? JournalEntryId { get; set; }

    public bool IsCreditNote { get; set; }
    public Guid? OriginalInvoiceId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Invoice? OriginalInvoice { get; set; }
    public ICollection<Invoice> CreditNotes { get; set; } = [];
    public ICollection<InvoiceLine> Lines { get; set; } = [];
    public ICollection<InvoicePayment> Payments { get; set; } = [];
    public ICollection<InvoiceReminder> Reminders { get; set; } = [];
}
