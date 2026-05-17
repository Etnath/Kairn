# US3: Payment Scheduling and Bill Payment

**As a** business owner or bookkeeper,
**I want** to see a list of upcoming bills due for payment and mark them as paid,
**So that** I can manage cash flow and avoid late payments.

## Acceptance Criteria

- [ ] A "Payment Schedule" view shows all Approved bills grouped by due date, sorted ascending, with columns: due date, vendor, reference, amount, and days until due.
- [ ] Bills due within the next 7 days are highlighted in amber; overdue bills are highlighted in red.
- [ ] The dashboard alerts panel shows a count of bills due within 7 days.
- [ ] A "Record Payment" button on a bill opens a payment form: payment date, amount (defaults to outstanding balance), payment account (bank/cash COA account), reference.
- [ ] On save, the system posts: debit Accounts Payable, credit Bank/Cash account.
- [ ] Bill status updates: full payment → **Paid**; partial payment → **Partially Paid**.
- [ ] A background job marks any Approved bill past its due date as **Overdue** nightly.
- [ ] Payment history is shown on the bill detail page.
- [ ] Payments can be reversed by Admin, which restores the prior bill status.

## Priority
Must Have

## Related Requirements
FR-AP-03, FR-DB-03
