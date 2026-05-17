# US1: Generate a Profit & Loss Statement

**As a** business owner or accountant,
**I want** to generate a Profit & Loss statement for any date range,
**So that** I can see whether the business made a profit or loss in that period and understand where money was earned and spent.

## Acceptance Criteria

- [ ] A "Profit & Loss" option is available under Reports.
- [ ] The user selects a date range using the `DateRangePicker` with presets: This Month, Last Month, This Quarter, YTD, Last Year, Custom.
- [ ] The report displays rows grouped in order: Revenue, Cost of Goods Sold (COGS), Gross Profit, Operating Expenses, EBITDA, Net Income.
- [ ] Each group shows individual account lines and a subtotal row.
- [ ] Gross Profit = Revenue − COGS; EBITDA = Gross Profit − Operating Expenses + Depreciation + Amortisation; Net Income = EBITDA − Interest − Tax.
- [ ] Positive P&L values (profit, revenue) are displayed in Lichen 700 `#1F6040`; negative values (loss, expense excess) are displayed in Signal 700 `#7E2A14`; zero values are displayed in Stone 500 `#8C8980`.
- [ ] The report title shows the selected date range clearly (e.g. "Profit & Loss — 1 January 2026 to 31 March 2026").
- [ ] The report loads in under 3 seconds for up to 100,000 transactions; a `MudProgressLinear` bar is shown during loading.
- [ ] A skeleton loader is shown immediately on page render before data arrives.

## Notes

- Account classification into P&L groups is determined by the Chart of Accounts account type and a configurable sub-type mapping.
- EBITDA is labelled "EBITDA" in the UI with a tooltip explaining the acronym.

## Priority
Must Have

## Related Requirements
FR-PL-01, NFR-P-01
