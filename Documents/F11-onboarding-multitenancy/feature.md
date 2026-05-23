# Feature: Onboarding & Multi-Tenancy

## Overview

The Onboarding & Multi-Tenancy module allows a single Kairn installation to host multiple independent companies (tenants). Each authenticated user may belong to one or more companies with a per-company role. When a user first signs in, a guided wizard collects the essential information needed to create a company and seed its chart of accounts and tax rates. Once inside the app, the user can switch between companies at any time, manage team members, or create additional companies.

## Goals

- Let each user manage multiple companies under a single Kairn account.
- Guide new users through company setup with a structured wizard so they land on a fully configured tenant.
- Enforce tenant isolation: every data query is scoped to the active `TenantId` in the auth cookie.
- Allow users to reset their own password without admin intervention.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-MT-01 | Each company is an independent tenant; all financial data is isolated by TenantId | Must Have |
| FR-MT-02 | A user may be a member of multiple tenants with a per-tenant role (Owner, Admin, Member, ReadOnly) | Must Have |
| FR-MT-03 | A first-run guard redirects unauthenticated or unassigned users to onboarding | Must Have |
| FR-MT-04 | A 4-step company creation wizard collects identity, legal status, and fiscal settings | Must Have |
| FR-MT-05 | The active company is displayed in the app bar with a dropdown to switch companies | Must Have |
| FR-MT-06 | An Owner or Admin can invite members to their company and assign roles | Should Have |
| FR-MT-07 | Users can request a password-reset email from the login screen | Must Have |

## Key Domain Rules

- The `tenant_id` claim is baked into the auth cookie at sign-in via `KairnUserClaimsPrincipalFactory`; all scoped services read it from `ICurrentUserContext`.
- Switching companies requires an HTTP round-trip through the `SelectTenant` Razor Page, which is the only place that can update the auth cookie (`SignInManager.RefreshSignInAsync`).
- A new company is seeded with a full chart of accounts (PCG) for Standard companies and a simplified subset for Auto-entrepreneurs (no VAT accounts, no fixed assets).
- SIREN numbers are validated with the Luhn algorithm before being stored.
- The VAT filing frequency field is hidden for Auto-entrepreneurs (they are exempt from VAT).

## Data Entities

- `Tenant` — Id (Guid, PK, never generated), Name, CreatedAt
- `TenantMembership` — TenantId + UserId (composite PK), Role, JoinedAt
- `ApplicationUser.ActiveTenantId` — nullable Guid pointing to the currently active tenant

## Technical References

- SDD §6.1 (Data Layer), §3.1 (Authentication / Identity)
- `KairnUserClaimsPrincipalFactory` — adds `tenant_id` claim
- `ICurrentUserContext` — reads `tenant_id` from HttpContext claims
- `SelectTenant.cshtml` Razor Page — the only correct place to refresh the auth cookie in Blazor Server
