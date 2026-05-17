# US3: Record Invoice Payments

**As a** bookkeeper or business owner,
**I want** to record full or partial payments against outstanding invoices,
**So that** the system automatically updates the invoice balance and posts the correct journal entries.

## Acceptance Criteria

- [ ] A "Record Payment" button is available on any invoice with status Sent, Partially Paid, or Overdue.
- [ ] The payment form has fields: payment date (required), amount (required, defaults to outstanding balance), payment method (Bank Transfer, Cash, Card, Other), reference (optional).
- [ ] The system validates that the payment amount does not exceed the outstanding balance; an error is shown if it does.
- [ ] On save, the system posts a journal entry: debit Bank/Cash account, credit Accounts Receivable.
- [ ] Invoice status updates automatically: if amount paid = total amount → **Paid**; if amount paid < total amount → **Partially Paid**.
- [ ] A payment history panel on the invoice detail page lists all payments: date, amount, method, reference.
- [ ] A payment can be deleted (reversed) by Admin, which posts a reversing journal entry and restores the prior invoice status.
- [ ] Payments are included in the audit log.

## Priority
Must Have

## Related Requirements
FR-AR-04, FR-AR-03
