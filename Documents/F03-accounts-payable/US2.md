# US2: Enter Vendor Bills

**As a** bookkeeper or business owner,
**I want** to enter vendor bills with line-item detail and expense categorisation,
**So that** the system posts the correct journal entries and I have an accurate record of what the business owes.

## Acceptance Criteria

- [ ] A "Bills" page lists all bills with filters for status, vendor, and due date; sortable by date and amount.
- [ ] An "Add Bill" button opens a bill entry form with fields: vendor (required, searchable), bill date (required), due date (auto-calculated from vendor payment terms, editable), reference number (vendor's invoice number), notes, and a line-item grid.
- [ ] Each bill line has: description (required), quantity, unit price, expense account (COA dropdown, defaults to vendor's default expense account), applicable tax rate, and computed line total.
- [ ] Bill totals section shows: subtotal, total tax, and grand total.
- [ ] Saving the bill in **Draft** status does not post journal entries.
- [ ] Clicking "Approve" (or "Post" if approval workflow is disabled) transitions the bill to **Approved** and posts journal entries: debit Expense accounts (per line), credit Accounts Payable; debit Input VAT account, credit AP for the tax portion.
- [ ] An optional receipt attachment (PDF or image, up to 10 MB) can be added to the bill.
- [ ] Viewer role can view bills but cannot create or approve them.

## Priority
Must Have

## Related Requirements
FR-AP-02, FR-AP-03
