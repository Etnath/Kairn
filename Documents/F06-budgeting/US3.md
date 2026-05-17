# US3: Year-End Budget Forecast

**As a** business owner,
**I want** to see a projection of where the business will end the fiscal year based on actuals to date and remaining budget,
**So that** I can anticipate whether I will hit my annual targets and take corrective action if needed.

## Acceptance Criteria

- [ ] A "Forecast" tab or section is available on the Budgeting page.
- [ ] The forecast is available for the current fiscal year only.
- [ ] For each account, the forecast is calculated as: `Actuals (months with data) + Budget (remaining months)`.
- [ ] The report displays: Account, YTD Actual, Remaining Budget, Full-Year Forecast, Annual Budget Target, and Forecast Variance (Forecast − Budget).
- [ ] A "as of" date picker lets the user simulate the forecast from a specific point in the year (e.g. end of Q3).
- [ ] The section subtotals (Revenue, COGS, Gross Profit, OpEx, Net Income) show forecast values and how they compare to the annual budget.
- [ ] An unfavourable forecast variance (net income forecast below budget) is shown in red with a summary warning: "Year-end net income is forecast at [amount], [X]% below budget."
- [ ] Export as CSV is available.

## Priority
Should Have

## Related Requirements
FR-BU-03
