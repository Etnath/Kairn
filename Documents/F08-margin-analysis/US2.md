# US2: Gross Margin Report

**As a** business owner,
**I want** to see the gross margin for each product or service line over a selected period,
**So that** I can identify which lines generate the most profit and which are underperforming.

## Acceptance Criteria

- [ ] A "Margin Analysis" page is accessible from the main navigation.
- [ ] The user selects a date range using `DateRangePicker`; presets match those on the P&L report.
- [ ] The report table shows one row per active product line with columns: product/service name, revenue, COGS, gross profit, and gross margin %.
- [ ] Gross Profit = Revenue − COGS; Gross Margin % = Gross Profit / Revenue × 100.
- [ ] A totals row at the bottom aggregates all lines.
- [ ] Rows with gross margin below the configured alert threshold are highlighted with Signal 50 `#FAECE8` background and Signal 700 `#7E2A14` text (matching the overdue row tint pattern from the color system).
- [ ] Product lines with zero revenue in the period are shown with 0% margin (not excluded).
- [ ] The report is computed from journal lines tagged to the linked accounts; computation completes in under 3 seconds.
- [ ] Export buttons for PDF and CSV are available.

## Priority
Must Have

## Related Requirements
FR-MA-02, NFR-P-01
