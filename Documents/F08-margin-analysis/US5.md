# US5: Margin Threshold Alerts

**As a** business owner,
**I want** to receive an alert when a product line's gross margin falls below a threshold I have set,
**So that** I can take corrective action before a loss-making line damages the overall business.

## Acceptance Criteria

- [ ] A margin alert threshold % is configurable per product line (see US1).
- [ ] When the system computes the current month's gross margin for a product line (during nightly job or on demand) and finds it below the threshold, an alert entry is created in the alerts panel.
- [ ] The alert message states: "[Product Line Name] gross margin is X% — below your threshold of Y%."
- [ ] Each unique product-line breach creates one alert per month; duplicate alerts for the same line and month are suppressed.
- [ ] Alerts are dismissible by the user; dismissed alerts do not reappear unless the threshold is breached again in a subsequent month.
- [ ] The nightly margin check runs after the nightly depreciation and status-update jobs.

## Priority
Nice to Have

## Related Requirements
FR-MA-05, FR-DB-03
