# US2: Period Comparison on the P&L

**As a** business owner,
**I want** to compare two P&L periods side-by-side,
**So that** I can instantly see whether the business is improving or declining relative to a prior period.

## Acceptance Criteria

- [ ] A "Compare" toggle on the P&L report enables a second date range picker labelled "Comparison Period".
- [ ] When enabled, the report adds three columns: Current Period, Comparison Period, and Variance (absolute CHF and percentage).
- [ ] Variance is calculated as: `Current − Comparison`; unfavourable variances (expense accounts over, revenue accounts under) are displayed in Signal 700 `#7E2A14`; favourable variances are displayed in Lichen 700 `#1F6040`.
- [ ] Presets for the comparison period are: Prior Month, Prior Quarter, Prior Year (relative to the primary period), and Custom.
- [ ] The report header shows both periods clearly, e.g. "Jan 2026 vs Jan 2025".
- [ ] Percentage variance is shown as "N/A" when the comparison period value is zero (division-by-zero guard).
- [ ] The comparison columns are printed/exported alongside the primary columns in PDF and CSV.

## Priority
Must Have

## Related Requirements
FR-PL-02, FR-PL-05
