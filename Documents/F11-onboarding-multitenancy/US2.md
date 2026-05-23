# US2: First-Run Guard

**As a** new user who has not yet created a company,
**I want** to be automatically redirected to the onboarding wizard when I sign in,
**So that** I never land on a blank or broken app state.

## Acceptance Criteria

- [ ] `MainLayout` checks the authenticated user's tenant memberships on every circuit start.
- [ ] If the user has **zero** memberships they are redirected to `/onboarding`.
- [ ] If the user has **one** membership but the auth cookie carries no valid `tenant_id`, the tenant is auto-selected via `/account/select-tenant` (HTTP redirect, updates the cookie).
- [ ] If the user has **two or more** memberships but no valid active tenant, they are redirected to `/select-company` to pick one.
- [ ] `SelectCompany.razor` lists all the user's companies as cards and navigates through `/account/select-tenant` on selection.
- [ ] `OnboardingLayout.razor` provides a minimal, centered layout (no nav drawer, no app bar) used by the onboarding and company-selection pages.
- [ ] `/account/select-tenant` is a Razor Page (GET) that validates membership, sets `ApplicationUser.ActiveTenantId`, calls `SignInManager.RefreshSignInAsync`, and redirects to `returnUrl`.
- [ ] Unauthenticated users are handled by the existing `RedirectToLogin` component (no change required).

## Priority
Must Have

## Related Requirements
FR-MT-03
