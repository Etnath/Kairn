using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public class InvoiceReminder : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public DateOnly SentDate { get; set; }
    public string SentByUserId { get; set; } = string.Empty;
    public string SentByName { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty; // "Email" | "Print" | "Bulk"

    public Invoice Invoice { get; set; } = null!;
}
