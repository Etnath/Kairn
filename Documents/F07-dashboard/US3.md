# US3: Dashboard Alerts Panel

**As a** business owner,
**I want** an alerts panel on the dashboard that highlights overdue invoices, upcoming payables, and low cash balance,
**So that** I am immediately informed of items requiring my attention without having to dig through each module.

## Acceptance Criteria

- [ ] An Alerts panel is displayed on the dashboard, below or alongside the charts.
- [ ] The panel displays alert items in three categories with distinct icons and colours per `Kairn_ColorSystem.md`:
  - **Overdue Invoices** — Signal 50 `#FAECE8` background, Signal 700 `#7E2A14` text and icon; shows count and total value; links to the AR aging report.
  - **Bills Due in 7 Days** — Summit 50 `#FBF2E0` background, Summit 700 `#8F6008` text and icon; shows count and total value; links to the AP payment schedule.
  - **Low Cash Balance** — Signal 50 `#FAECE8` background, Signal 700 `#7E2A14` text and icon; shown when Cash Balance < configurable threshold (default CHF 5,000); displays current balance and threshold.
- [ ] Margin threshold breaches (from Margin Analysis US5) use Summit 50 / Summit 700 styling (warning, not critical) and are also surfaced in the alerts panel when applicable.
- [ ] Each alert type shows "All clear" with a Lichen 500 `#3A9463` checkmark icon and Lichen 50 `#E8F4ED` background when no alert condition is active.
- [ ] The alerts panel refreshes every 5 minutes via Fluxor alongside the KPI cards.
- [ ] The cash balance alert threshold is configurable in Settings → Dashboard.
- [ ] Alerts are read-only in the panel; the user must navigate to the relevant module to take action.

## Priority
Must Have

## Related Requirements
FR-DB-03, FR-AR-05, FR-AP-03, FR-MA-05
