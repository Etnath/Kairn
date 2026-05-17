# US4: AR Aging Report

**As a** business owner,
**I want** to see a report of all outstanding receivables grouped by how overdue they are,
**So that** I can prioritise collection efforts and understand my credit exposure.

## Acceptance Criteria

- [ ] An "AR Aging" report is available under the Accounts Receivable module.
- [ ] The report is generated as of a selected date (defaults to today).
- [ ] The report shows one row per customer with columns: customer name, current (not yet due), 1–30 days overdue, 31–60 days, 61–90 days, 91+ days, and total outstanding.
- [ ] A totals row at the bottom sums each aging bucket.
- [ ] Clicking a customer row expands it to show the individual invoices contributing to each aging bucket.
- [ ] The report can be filtered by customer name.
- [ ] Export buttons for PDF and CSV are available; PDF is in landscape orientation.
- [ ] Customers with a zero outstanding balance are excluded from the report by default; a toggle shows them.

## Priority
Must Have

## Related Requirements
FR-AR-05
