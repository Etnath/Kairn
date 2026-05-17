# US5: Export the P&L Report

**As an** accountant or business owner,
**I want** to export the P&L report as PDF or CSV,
**So that** I can share it with stakeholders, file it for records, or open it in a spreadsheet for further analysis.

## Acceptance Criteria

- [ ] The P&L report toolbar contains two export buttons: "Export PDF" and "Export CSV".
- [ ] PDF export renders the report with the Kairn logo, report title, selected date range, and all rows at the current filter state (including comparison and budget columns if enabled) using QuestPDF.
- [ ] PDF page orientation is landscape if the report has more than 3 columns.
- [ ] CSV export includes a header row with column names and one data row per account line; subtotal rows are included and labelled.
- [ ] Both exports include the generation timestamp and the user's name in the footer/metadata.
- [ ] Exports are served as file downloads (Content-Disposition: attachment); the file name includes the period, e.g. `PL_2026-01_2026-03.pdf`.
- [ ] Export completes within 5 seconds for reports covering up to 100,000 transactions.

## Priority
Must Have

## Related Requirements
FR-PL-05, NFR-P-01, §5.5 (PDF Generation)
