# US4: Adaptive Navigation for Auto-Entrepreneur

**As an** auto-entrepreneur,
**I want** the application to hide accounting modules I am not legally required to use,
**So that** the interface is not overwhelming and I cannot accidentally use a feature that would produce a compliance issue.

## Acceptance Criteria

- [ ] When the business status is **Auto-entrepreneur**, the following navigation items and their associated pages are hidden (but their routes remain accessible by direct URL so that a user who opts in can still reach them):
  - **Tax & VAT** group (VAT Return, Tax Period Locking, Accountant Export with VAT summary)
  - **Tax Rate Configuration** under Settings
  - The **Balance Sheet** section within Financial Reports
  - The **Fixed Assets** nav item
  - The **Expense Reports** nav item (simplified auto-entrepreneurs typically do not have employees)
- [ ] The **Accountant Export** remains available in Auto-entrepreneur mode but generates only: Livre des recettes (CSV/PDF of all invoices for the year), P&L (simplified), and Journal Entries Excel. The VAT Summary file is omitted.
- [ ] The Dashboard suppresses the VAT net payable KPI card and the AR/AP aging widgets, replacing them with the TVA threshold widget from US3.
- [ ] A persistent banner at the top of the Settings page (visible to Admin only) shows the current business status with a link to change it.
- [ ] When the status is changed from Auto-entrepreneur back to Standard, all hidden items reappear immediately without requiring a page reload (driven by a cascading context provider).

## Priority
Must Have

## Related Requirements
FR-CP-04
