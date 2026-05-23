# US5: Team Management

**As an** Owner or Admin of a company,
**I want** to invite other users to my company and assign them roles,
**So that** my team can collaborate in Kairn with appropriate access levels.

## Acceptance Criteria

- [ ] A "Team" page is accessible under Settings, visible only to members with the Owner or Admin role.
- [ ] The page lists all current members: name/email, role, date joined.
- [ ] An Owner or Admin can invite a new member by entering an email address and selecting a role (Admin, Member, ReadOnly).
- [ ] If the email matches an existing Kairn user, a `TenantMembership` is created immediately and the user receives an email notification.
- [ ] If the email does not match a known user, an invitation email is sent; on registration the membership is auto-provisioned.
- [ ] An Owner can change any member's role except their own.
- [ ] An Admin can change Member/ReadOnly roles but cannot promote to Owner or Admin.
- [ ] Any member can be removed by an Owner; an Admin can remove Members and ReadOnly members only.
- [ ] The Owner role cannot be removed; there must always be exactly one Owner per company.
- [ ] All membership changes are recorded in the audit log.
- [ ] All labels are localised (EN + FR).

## Priority
Should Have

## Related Requirements
FR-MT-02, FR-MT-06
