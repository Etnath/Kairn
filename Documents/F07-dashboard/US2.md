# US2: Dashboard Charts

**As a** business owner,
**I want** to see visual charts of revenue vs. expenses and cash position on the dashboard,
**So that** I can identify trends at a glance without generating a formal report.

## Acceptance Criteria

- [ ] The dashboard displays three charts below the KPI cards, implemented with ApexCharts.Blazor, using the chart palette from `Kairn_ColorSystem.md`:
  1. **Revenue vs. Expenses bar chart** — grouped bars for each of the last 12 calendar months; Revenue bars use Lichen 500 `#3A9463` (Series 1), Expenses bars use Signal 500 `#C2492A` (Danger/negative area).
  2. **P&L trend line chart** — net profit line for each of the last 12 months; months with positive net income are filled Lichen 500; months with negative net income are filled Signal 500.
  3. **Cash position line chart** — end-of-month cash balance for the last 12 months; line uses Slate 500 `#4D7A9E` (Series 2).
  - Chart grid lines use Stone 200 `#D6D3CA`; axis labels use Stone 500 `#8C8980`.
- [ ] Clicking a month's bar on the Revenue vs. Expenses chart navigates to the P&L report pre-filtered to that month.
- [ ] Charts are lazy-loaded after the KPI cards; a skeleton is shown during fetch.
- [ ] Chart tooltips show the full value formatted as currency on hover.
- [ ] Charts are responsive and render correctly at 768px+ screen width.
- [ ] Data for all three charts is fetched in a single server call to minimise round-trips.

## Priority
Must Have

## Related Requirements
FR-DB-02
