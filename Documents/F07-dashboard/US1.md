# US1: KPI Cards

**As a** business owner,
**I want** to see key financial KPIs on the dashboard as soon as I log in,
**So that** I can assess the health of the business in seconds without navigating into individual modules.

## Acceptance Criteria

- [ ] The Dashboard page (default route `/`) displays six KPI cards: Monthly Revenue, Monthly Expenses, Net Profit, Outstanding AR, Outstanding AP, and Cash Balance.
- [ ] Each card uses the `KpiCard` component showing: title, current-period value (formatted as currency with symbol), delta % vs. the prior month, and a trend arrow. For Revenue, Net Profit, and Cash Balance: an upward arrow uses Lichen 700 `#1F6040` (favourable) and a downward arrow uses Signal 700 `#7E2A14` (unfavourable). For Outstanding AR and AP the logic is inverted (a decrease is favourable).
- [ ] Monthly Revenue = total revenue-account credits in the current calendar month.
- [ ] Monthly Expenses = total expense-account debits in the current calendar month.
- [ ] Net Profit = Monthly Revenue − Monthly Expenses.
- [ ] Outstanding AR = sum of unpaid invoice balances (status: Sent, Partially Paid, Overdue).
- [ ] Outstanding AP = sum of unpaid bill balances (status: Approved, Partially Paid, Overdue).
- [ ] Cash Balance = sum of all bank/cash account balances as of today.
- [ ] KPI data is loaded asynchronously; `MudSkeleton` placeholders are shown during the fetch.
- [ ] KPI values refresh every 5 minutes via a Fluxor dispatch while the tab is active.

## Priority
Must Have

## Related Requirements
FR-DB-01
