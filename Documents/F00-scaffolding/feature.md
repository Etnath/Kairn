# Feature: Application Scaffolding

## Overview

Establish the foundational Blazor Server application structure for Kairn, including project layout, dependencies, database connectivity, authentication skeleton, shell UI, and CI/CD pipeline. All subsequent features are built on top of this scaffold.

## Goals

- Create the complete solution and project structure matching the SDD specification.
- Wire up the technology stack end-to-end (Blazor Server, EF Core, PostgreSQL, MudBlazor, Fluxor, Serilog).
- Provide a working login/logout flow and role-based access control skeleton.
- Establish the shell layout (nav drawer, app bar, main content area) with placeholder pages for each module.
- Ensure the application is containerised and can be deployed via Docker Compose from day one.
- Set up the full localisation infrastructure so that every UI string is externalised from day one and both English and French are supported from the first release.

## Requirements

### Functional
- Solution contains five projects: `Kairn.Domain`, `Kairn.Application`, `Kairn.Infrastructure`, `Kairn.Blazor`, `Kairn.Tests`.
- Base layered architecture is in place (Presentation → Application → Domain ← Infrastructure).
- ASP.NET Core Identity with cookie authentication is configured (login, logout, session timeout).
- Role definitions: Admin, Bookkeeper, Viewer are seeded.
- `AppDbContext` is configured for PostgreSQL (production) and SQLite (development/test) using EF Core 8.
- EF Core migrations run automatically on startup.
- A seed migration provides default Chart of Accounts structure.
- `MainLayout.razor` renders a persistent left-drawer nav and top app bar with all module links.
- Placeholder routable pages exist for every module.
- Health check endpoint `GET /health` is registered.
- Serilog structured logging is configured with console and file sinks.
- `Result<T>` pattern, `BaseEntity`, and `AuditLog` interceptor are implemented in shared layers.
- Fluxor is registered and a minimal `DashboardStore` stub is present.

### Localisation
- `Microsoft.Extensions.Localization` is registered and `RequestLocalizationOptions` supports cultures `en` and `fr` (default: `en`).
- All user-facing strings in shell components (`MainLayout`, `NavMenu`, login page, placeholder pages) are stored in `.resx` resource files; no hard-coded English strings in `.razor` or `.cs` files.
- Resource files follow the path `Resources/Pages/`, `Resources/Shared/`, and `Resources/Components/` mirroring the component folder structure.
- A `LanguageSwitcher` component in the top app bar lets the user toggle between English and French without a full page reload.
- The selected locale is persisted in a `lang` cookie (30-day expiry) and reapplied on every request via `RequestLocalizationMiddleware`.
- Number formatting (decimal separator, thousands separator) and date formatting adapt to the active locale (`fr` → `dd/MM/yyyy`, `,` decimal; `en` → `MM/dd/yyyy`, `.` decimal).
- Currency symbol display follows locale conventions; the CHF symbol is always shown for Swiss franc amounts regardless of locale.

### Non-Functional
- Application starts cleanly in development (`dotnet run`) and in Docker (`docker compose up`).
- HTTPS is enforced; HSTS header is present.
- Tailwind CSS JIT build pipeline is configured.
- GitHub Actions CI workflow runs `dotnet build` and `dotnet test` on every push.
- 80% unit test coverage target is established (even if no domain tests exist yet beyond a smoke test).

## Out of Scope

All business logic for individual modules (General Ledger, Reporting, AR, AP, etc.) is out of scope for this feature and is addressed in subsequent feature stories.

## Technical References

- SDD §2 (Architectural Overview), §3 (Solution Structure), §5 (Server-Side & API Design), §8 (Security Design), §10 (Deployment & Infrastructure)
- SRS §2.4 (Operating Environment), §4.2 (Security NFRs), §4.3 NFR-U-03 (Localisation), §4.4 (Reliability), §4.5 (Maintainability)
- Kairn_ColorSystem.md — all MudBlazor theme values and Tailwind color ramps are authoritative from this document, not from SDD §7.2 (which is superseded). Both the light and dark `KairnTheme` palettes must be configured in the scaffold.
