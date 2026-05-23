# US4: Company Switcher

**As a** user who belongs to multiple companies,
**I want** to switch between my companies from within the app without signing out,
**So that** I can manage several businesses in a single session.

## Acceptance Criteria

- [ ] The active company name is displayed in the app bar, between the "Kairn" title and the spacer, as a clickable dropdown button.
- [ ] The dropdown lists all companies the user belongs to, each showing the company name and the user's role in that company.
- [ ] The currently active company is highlighted with a checkmark.
- [ ] Clicking a different company navigates through `/account/select-tenant` (forceLoad, HTTP redirect) so the auth cookie is updated correctly.
- [ ] Clicking the active company closes the menu without any navigation.
- [ ] A "New company…" entry at the bottom of the dropdown navigates to `/onboarding`.
- [ ] `TenantProfileState` exposes `CompanyName` so the switcher always shows the correct name after a profile update.
- [ ] The component renders nothing if the user has no memberships (prevents errors in the onboarding/select-company flow, which use a different layout).
- [ ] All labels are localised (EN + FR).

## Priority
Must Have

## Related Requirements
FR-MT-01, FR-MT-02, FR-MT-05
