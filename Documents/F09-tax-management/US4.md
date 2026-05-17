# US4: Accountant Export Package

**As a** business owner,
**I want** to export a complete financial data package for any period in a single action,
**So that** I can hand over everything my external accountant needs without manually assembling reports.

## Acceptance Criteria

- [ ] An "Export for Accountant" button is available in the Tax Management module.
- [ ] The user selects a fiscal year or custom date range and clicks "Generate Export".
- [ ] The system generates a ZIP archive containing:
  - `TrialBalance_[period].pdf` and `.csv`
  - `ProfitLoss_[period].pdf` and `.csv`
  - `BalanceSheet_[period].pdf` and `.csv`
  - `JournalEntries_[period].xlsx` (full journal entry listing with all lines)
  - `VATSummary_[period].pdf` and `.csv`
- [ ] All PDF documents use the QuestPDF renderer and include the business name, period, and generation timestamp.
- [ ] The Excel journal entries file includes columns: date, reference, description, account code, account name, debit, credit, currency, exchange rate, and memo.
- [ ] The ZIP file is served as a download with name: `Kairn_AccountantExport_[YYYY]_[period].zip`.
- [ ] Generation of the export completes within 30 seconds for a full fiscal year of up to 100,000 entries.
- [ ] An Admin or Bookkeeper can trigger the export; Viewer role cannot.

## Priority
Must Have

## Related Requirements
FR-TX-04, §5.5 (PDF — QuestPDF), §5.6 (Excel — ClosedXML)
