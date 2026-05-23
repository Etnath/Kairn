# US6: Password Reset

**As a** user who has forgotten their password,
**I want** to request a password-reset link from the login page,
**So that** I can regain access to my account without administrator intervention.

## Acceptance Criteria

- [ ] A "Forgot password?" link is visible on the login page.
- [ ] Clicking the link opens a page with a single email field and a "Send reset link" button.
- [ ] Submitting a valid email triggers `UserManager.GeneratePasswordResetTokenAsync` and sends a reset email via `IEmailService`; the link contains the encoded token and email as query parameters.
- [ ] If the email is not found, the same success message is displayed (no user enumeration).
- [ ] The reset link navigates to a "Reset password" page that accepts the new password and confirmation, validates them (min 8 chars, matching), and calls `UserManager.ResetPasswordAsync`.
- [ ] On success the user is shown a confirmation message with a link back to the login page.
- [ ] On failure (expired or invalid token) a clear error message is shown; the user is prompted to request a new link.
- [ ] The reset email is localised (EN + FR) using the same `IEmailService` used elsewhere.
- [ ] All pages use the `OnboardingLayout` (no nav drawer, centered card).

## Priority
Must Have

## Related Requirements
FR-MT-07
