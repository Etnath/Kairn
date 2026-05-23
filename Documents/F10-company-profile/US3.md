# US3: TVA Threshold Alert

**As an** auto-entrepreneur,
**I want** to be warned before my annual revenue reaches the franchise en base de TVA threshold,
**So that** I have time to plan the transition to VAT registration and avoid a surprise tax liability.

## Acceptance Criteria

- [ ] When the business status is **Auto-entrepreneur**, the system tracks YTD revenue: the sum of all non-void invoices dated within the current calendar year.
- [ ] The applicable threshold is determined by the Activity Type stored in the Company Profile (Services: 77 700 €; Commercial: 188 700 €), or the tenant's custom override values.
- [ ] The Dashboard displays a **TVA Threshold** KPI widget showing YTD revenue, the applicable threshold, and a colour-coded progress bar:
  - Below 80 %: neutral / grey
  - 80 %–99 %: amber warning
  - 100 %+: red alert
- [ ] When YTD revenue first crosses **80 %** of the threshold, an in-app notification is generated (same mechanism as existing margin alerts): "Your annual revenue has reached X % of the TVA franchise threshold (Y €). You must register for VAT if you exceed Z €."
- [ ] When YTD revenue first crosses **100 %** of the threshold, a second in-app notification is generated: "You have exceeded the TVA franchise threshold. You are now required to charge VAT on invoices from the 1st of next month. Update your business status in Settings."
- [ ] Each threshold crossing generates at most one notification per calendar year (de-duplicated by year + threshold level).
- [ ] The threshold widget and notifications are entirely hidden when business status is **Standard**.

## Priority
Must Have

## Related Requirements
FR-CP-03
