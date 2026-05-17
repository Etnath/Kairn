# US1: Create an Annual Budget

**As a** business owner,
**I want** to enter a budget for each account broken down by month,
**So that** I have a financial plan to compare against actual results throughout the year.

## Acceptance Criteria

- [ ] A "Budgets" page is accessible from the main navigation.
- [ ] An Admin or Bookkeeper can create a new budget with fields: fiscal year (required), budget name (required, e.g. "2026 Operating Budget").
- [ ] Once created, the budget opens a spreadsheet-style entry grid with rows for each Revenue and Expense account from the COA and columns for each month (Jan–Dec, or configured fiscal year months).
- [ ] Each cell is a numeric input for the budgeted amount; cells default to 0.00.
- [ ] A row total and column total are computed and displayed read-only.
- [ ] An annual total per account is shown in a rightmost column.
- [ ] The budget is saved incrementally (auto-save or explicit Save button); partial budgets are allowed.
- [ ] Only one budget per fiscal year is supported in v1.0; attempting to create a second shows: "A budget already exists for [year]. Edit the existing budget instead."
- [ ] Viewer role can view budgets but cannot edit them.

## Priority
Must Have

## Related Requirements
FR-BU-01
