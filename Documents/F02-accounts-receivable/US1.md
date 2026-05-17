# US1: Customer Management

**As a** business owner or bookkeeper,
**I want** to maintain a database of customers with their contact details and payment terms,
**So that** I can quickly create invoices and track each customer's outstanding balance and credit status.

## Acceptance Criteria

- [ ] A "Customers" section is accessible within the Accounts Receivable module.
- [ ] The customer list shows a searchable, sortable, paginated table with columns: name, email, payment terms (days), credit limit, outstanding balance, and currency.
- [ ] An Admin or Bookkeeper can create a new customer with fields: name (required), email, address, tax number, payment terms in days (default 30), credit limit (optional), default currency (CHF).
- [ ] A customer record can be edited at any time; the name is the only required field.
- [ ] Deactivating a customer prevents new invoices from being issued to them but preserves all history.
- [ ] The outstanding balance displayed on the customer list is computed from unpaid invoice totals in real time.
- [ ] Clicking a customer row opens a customer detail page showing contact info, all invoices (with status), and the total outstanding balance.
- [ ] All customer record changes are recorded in the audit log.

## Priority
Must Have

## Related Requirements
FR-AR-01
