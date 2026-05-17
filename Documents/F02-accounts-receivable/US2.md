# US2: Create and Send Invoices

**As a** business owner or bookkeeper,
**I want** to create itemised invoices for customers and send them directly from the application,
**So that** customers receive professional invoices promptly and I have a record of what has been billed.

## Acceptance Criteria

- [ ] An "Invoices" page lists all invoices with filters for status and customer; sortable by date, amount, and due date.
- [ ] An "Add Invoice" button opens the `InvoiceForm` with fields: customer (required, searchable), invoice date (required, defaults to today), due date (auto-calculated from customer payment terms, editable), reference number (auto-generated, editable), notes field, and a line-item grid.
- [ ] Each invoice line has: description (required), quantity, unit price, discount %, applicable tax rate (dropdown), and computed line total shown read-only.
- [ ] Invoice totals section shows: subtotal, total discount, total tax (VAT), and grand total.
- [ ] Saving a new invoice creates it in **Draft** status; a GL journal entry is not posted until the invoice is sent.
- [ ] Clicking "Send" transitions the invoice from Draft to **Sent** and posts the corresponding AR journal entries (debit AR, credit Revenue; debit Revenue, credit VAT Payable for tax portion).
- [ ] Sending via SMTP (if configured) attaches the invoice as a PDF and opens a confirmation dialog before sending.
- [ ] A sent invoice can be viewed as a PDF preview within the application.
- [ ] The invoice PDF uses the Kairn design theme and includes business details, customer details, line items, totals, and payment instructions.
- [ ] Viewer role can view invoices but cannot create or send them.

## Priority
Must Have

## Related Requirements
FR-AR-02, FR-AR-03, §5.4 (SMTP), §5.5 (PDF Generation)
