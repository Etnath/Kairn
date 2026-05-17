# US1: Generate a Balance Sheet Report

**As a** business owner or accountant,
**I want** to generate a Balance Sheet for any selected date,
**So that** I can see a complete snapshot of the business's assets, liabilities, and equity at that point in time.

## Acceptance Criteria

- [ ] A "Balance Sheet" option is available under Reports.
- [ ] The user selects a single "as of" date (defaults to today).
- [ ] The report displays three sections: Assets, Liabilities, and Equity, each with current and non-current sub-sections.
- [ ] Each section shows individual account lines with account code, name, and balance.
- [ ] Section subtotals are shown: Total Current Assets, Total Non-Current Assets, Total Assets, Total Current Liabilities, Total Non-Current Liabilities, Total Liabilities, Total Equity.
- [ ] A footer row shows `Total Assets = Total Liabilities + Total Equity`; if this does not hold, a red warning is displayed.
- [ ] The report loads in under 3 seconds for up to 100,000 transactions.
- [ ] Export buttons for PDF and CSV are available in the toolbar; PDF is in portrait orientation.

## Notes

- Asset and liability accounts are classified as current or non-current based on a configurable flag on the account record (added during US1 of General Ledger feature).
- Equity section includes: Share Capital, Retained Earnings (auto-calculated from prior periods' net income), and Current Year Earnings.

## Priority
Must Have

## Related Requirements
FR-BS-01, FR-PL-01, NFR-P-01
