# US1: Company Profile Settings

**As an** administrator,
**I want** to configure the business's legal identity and status in one place,
**So that** the system can adapt its behaviour and compliance requirements to the actual legal regime of the company.

## Acceptance Criteria

- [ ] A "Company Profile" section exists under Settings, accessible only to Admin.
- [ ] The form includes: Legal Name (required), SIRET (14 digits, validated format), Street Address, Postal Code, City, Country, Business Status radio (Standard / Auto-entrepreneur), Activity Type (Services / Liberal profession | Commercial / Accommodation — only visible when status is Auto-entrepreneur), and Logo upload (PNG/JPG, max 1 MB).
- [ ] When status is **Auto-entrepreneur**, two additional fields appear: VAT Threshold Services (pre-filled 77 700 €) and VAT Threshold Commercial (pre-filled 188 700 €), both editable so the user can update for future fiscal years without a code change.
- [ ] Saving the profile persists to a `TenantProfile` row (upsert); changes take effect immediately across all open sessions of that tenant.
- [ ] A success snackbar confirms the save; validation errors are shown inline.
- [ ] All profile changes are recorded in the audit log with old and new values.
- [ ] The company name and SIRET are printed on PDF invoices and reports (replaces the current hard-coded "Kairn" placeholder).

## Priority
Must Have

## Related Requirements
FR-CP-01
