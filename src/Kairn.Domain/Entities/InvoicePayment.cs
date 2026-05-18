using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum PaymentMethod { BankTransfer, Cash, Card, Other }

public class InvoicePayment : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public Guid? JournalEntryId { get; set; }

    public Invoice Invoice { get; set; } = null!;
}
