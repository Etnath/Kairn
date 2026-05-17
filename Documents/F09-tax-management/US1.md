# US1: Configure VAT Rates

**As an** administrator,
**I want** to configure VAT rates by category with effective date ranges,
**So that** the correct tax rate is applied automatically to invoices and bills based on the transaction date and category.

## Acceptance Criteria

- [ ] A "Tax Rates" section is accessible under Settings.
- [ ] An Admin can create a tax rate with fields: name (required, e.g. "Standard VAT"), rate % (required), category (e.g. Standard, Reduced, Exempt), valid from date (required), valid to date (optional, blank = indefinite), and default flag.
- [ ] Multiple rates with different valid-date ranges are allowed for the same category (to handle rate changes).
- [ ] Only one rate per category can be marked as default at a time; marking a new one as default unsets the previous.
- [ ] Tax rates are applied to invoice/bill lines via a dropdown in the line-item form; the applicable rate for the transaction date is pre-selected when the line uses the default category.
- [ ] An existing rate cannot be edited once it has been applied to a posted transaction; a new rate with a new effective date must be created instead.
- [ ] The seed migration includes Swiss VAT rates: Standard 8.1%, Reduced 2.6%, Exempt 0%.
- [ ] All tax rate changes are recorded in the audit log.

## Priority
Must Have

## Related Requirements
FR-TX-01
