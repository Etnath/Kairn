# US1: Manage the Chart of Accounts

**As a** business owner or accountant,
**I want** to create, edit, and organise accounts in a Chart of Accounts,
**So that** every financial transaction can be classified correctly and reports reflect accurate account balances.

## Acceptance Criteria

- [ ] A Settings → Chart of Accounts page lists all accounts in a hierarchical tree grouped by type: Assets, Liabilities, Equity, Revenue, Expenses.
- [ ] Each account displays: code, name, type, parent account, currency, and active/inactive status.
- [ ] An Admin or Bookkeeper can create a new account via a modal form with fields: code (required, unique), name (required), type (required), parent account (optional), currency (default: CHF), active (default: true).
- [ ] An Admin or Bookkeeper can edit the name, parent, currency, and active flag of an existing account; the code and type are immutable once the account has transactions.
- [ ] Deactivating an account hides it from new transaction entry dropdowns but preserves its historical balances.
- [ ] Attempting to create a duplicate account code returns a validation error: "Account code already exists."
- [ ] The COA page supports search/filter by code or name.
- [ ] Viewer role can view the COA but cannot create or edit accounts.
- [ ] All COA changes are recorded in the audit log.

## Notes

- Account codes follow a numeric convention (e.g. 1000–1999 Assets, 2000–2999 Liabilities, etc.) but the system does not enforce a specific numbering scheme beyond uniqueness.
- The seed migration provides a default Swiss COA; users may extend or modify it.

## Priority
Must Have

## Related Requirements
FR-GL-01, FR-GL-07
