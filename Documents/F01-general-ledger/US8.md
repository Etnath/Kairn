# US8: Account Drill-Down and Ledger Filtering

**As a** bookkeeper or accountant,
**I want** to filter the ledger by account, date range, and reference, and drill into a specific account's transaction history,
**So that** I can investigate individual account activity quickly without exporting to a spreadsheet.

## Acceptance Criteria

- [ ] The General Ledger page toolbar includes filters for: date range (using `DateRangePicker`), account (multi-select COA dropdown), reference (text search), and created-by user.
- [ ] Filters apply instantly (debounced 300 ms) without a full page reload.
- [ ] Each account in the COA tree has a "View Ledger" link that opens the General Ledger pre-filtered to that account.
- [ ] The ledger table shows columns: date, reference, description, debit, credit, running balance, and memo.
- [ ] Running balance is computed cumulatively within the filtered result set for a single-account filter; it is hidden for multi-account views.
- [ ] Clicking a row expands it inline to show all lines of the parent journal entry and the attachment thumbnail (if any).
- [ ] The table supports pagination (default 50 rows per page) with a page-size selector.
- [ ] The filtered ledger can be exported as CSV.

## Notes

- Running balance is calculated server-side; the query orders by date ASC, then by entry ID for tie-breaking.
- Multi-account filters disable the running balance column and show a tooltip explaining why.

## Priority
Must Have

## Related Requirements
FR-GL-01, FR-GL-02, FR-GL-03
