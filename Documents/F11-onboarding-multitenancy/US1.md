# US1: Multi-Tenant Data Model

**As a** developer,
**I want** a robust multi-tenant data model with per-user company memberships and role assignments,
**So that** every authenticated request is correctly scoped to a single company and users can belong to multiple companies.

## Acceptance Criteria

- [ ] A `Tenant` entity exists with Id (Guid, `ValueGeneratedNever`), Name, CreatedAt.
- [ ] A `TenantMembership` join table has a composite PK of (TenantId, UserId), a `TenantRole` enum (Owner, Admin, Member, ReadOnly), and a JoinedAt timestamp.
- [ ] `ApplicationUser` has a nullable `ActiveTenantId` (Guid?) replacing any prior single `TenantId` field; the migration preserves existing values.
- [ ] `KairnUserClaimsPrincipalFactory` adds a `tenant_id` claim from `user.ActiveTenantId` into the auth cookie at sign-in.
- [ ] `ICurrentUserContext` reads `TenantId` from the `tenant_id` claim so all scoped services remain unchanged.
- [ ] `ITenantMembershipService` exposes: `GetUserMembershipsAsync`, `IsMemberAsync`, `AddMemberAsync`.
- [ ] The dev seeder creates a demo tenant (Id = `Guid.Empty`, Name = "Kairn Demo") and assigns the dev admin user as Owner.
- [ ] All existing financial data queries continue to work without modification.

## Priority
Must Have

## Related Requirements
FR-MT-01, FR-MT-02
