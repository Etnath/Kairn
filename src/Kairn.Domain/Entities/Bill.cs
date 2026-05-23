using Kairn.Domain.Common;

namespace Kairn.Domain.Entities;

public enum BillStatus { Draft, PendingApproval, Approved, PartiallyPaid, Paid, Overdue, Void, Rejected }

public class Bill : BaseEntity
{
    public Guid VendorId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateOnly DueDate { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Draft;
    public string Currency { get; set; } = "EUR";
    public string? Notes { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string? RejectionReason { get; set; }

    public Vendor Vendor { get; set; } = null!;
    public ICollection<BillLine> Lines { get; set; } = [];
    public ICollection<BillAttachment> Attachments { get; set; } = [];
    public ICollection<BillPayment> Payments { get; set; } = [];
}

public class BillPayment : BaseEntity
{
    public Guid BillId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public Guid? JournalEntryId { get; set; }

    public Bill Bill { get; set; } = null!;
}

public class BillLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BillId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal TaxRate   { get; set; }
    public Guid?   TaxRateId { get; set; }
    public Guid    ExpenseAccountId { get; set; }
    public int SortOrder { get; set; }

    public Bill Bill { get; set; } = null!;
    public Account ExpenseAccount { get; set; } = null!;

    public decimal NetAmount => Quantity * UnitPrice;
    public decimal TaxAmount => NetAmount * TaxRate / 100m;
    public decimal LineTotal => NetAmount + TaxAmount;
}

public class BillAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BillId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
    public DateTimeOffset UploadedAt { get; set; }

    public Bill Bill { get; set; } = null!;
}
