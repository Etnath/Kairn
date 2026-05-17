# US1: Vendor Management

**As a** bookkeeper or business owner,
**I want** to maintain a database of vendors with their contact details, bank information, and payment terms,
**So that** I can quickly enter bills and schedule payments to the right vendor accounts.

## Acceptance Criteria

- [ ] A "Vendors" section is accessible within the Accounts Payable module.
- [ ] The vendor list shows a searchable, sortable table with columns: name, contact email, payment terms (days), outstanding balance, and default expense account.
- [ ] An Admin or Bookkeeper can create a vendor with fields: name (required), contact email, address, IBAN/bank details, payment terms in days (default 30), default expense account (COA account selector).
- [ ] A vendor record can be edited at any time.
- [ ] Deactivating a vendor prevents new bills from being entered for them but preserves all history.
- [ ] Outstanding balance is computed from unpaid bills in real time.
- [ ] Clicking a vendor row opens a detail page with contact info, all bills (with status), and the total outstanding balance.
- [ ] All vendor record changes are recorded in the audit log.

## Priority
Must Have

## Related Requirements
FR-AP-01
