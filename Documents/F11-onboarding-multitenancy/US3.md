# US3: Company Creation Wizard

**As a** user without a company,
**I want** a guided wizard that collects the essential information about my company,
**So that** my workspace is fully configured (chart of accounts, tax rates, fiscal settings) from day one.

## Acceptance Criteria

- [ ] The wizard at `/onboarding` has four steps with a visual step indicator (numbered circles + connecting line).
- [ ] **Step 1 — Company identity:** company name (required), legal form dropdown (EURL/SARL/SAS/SASU/SA/EI/Micro-entreprise/Association), SIREN (optional, Luhn-validated), address, postal code, city.
- [ ] **Step 2 — Business status:** card toggle between Standard and Auto-entrepreneur, activity type radio (Services / Commercial), contextual alert for AE+Commercial explaining the higher revenue threshold.
- [ ] **Step 3 — Fiscal settings:** fiscal year start month (month picker 1–12), VAT filing frequency (Monthly/Quarterly/Annual); the VAT frequency field is hidden for Auto-entrepreneurs with an informational note.
- [ ] **Step 4 — Review:** read-only summary table of all entered values, "Create company" button with loading spinner.
- [ ] On creation: a new `Tenant` is inserted, a `TenantProfile` is seeded with all collected values, a chart of accounts is seeded (full PCG for Standard, simplified AE subset for Auto-entrepreneurs), five standard French TVA rates are seeded, and the user is added as Owner.
- [ ] After creation the user is redirected through `/account/select-tenant` to update the auth cookie, then lands on `/`.
- [ ] SIREN validation: 9 digits, Luhn algorithm; the field is optional (blank passes).
- [ ] All labels and messages are localised (EN + FR).

## Priority
Must Have

## Related Requirements
FR-MT-04
