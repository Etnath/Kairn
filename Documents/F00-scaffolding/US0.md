# US0: Scaffold the Base Kairn Application

**As a** developer,
**I want** a fully configured Blazor Server solution with all core dependencies wired up, a working login/logout flow, shell UI, and Docker deployment,
**So that** every subsequent feature can be built on a stable, consistent foundation without re-solving infrastructure concerns.

## Acceptance Criteria

- [ ] Solution `Kairn.sln` contains five projects: `Kairn.Domain`, `Kairn.Application`, `Kairn.Infrastructure`, `Kairn.Blazor`, `Kairn.Tests`.
- [ ] Dependency direction is enforced: Blazor → Application → Domain; Infrastructure → Domain; no upward references.
- [ ] `AppDbContext` is configured with EF Core 8 targeting PostgreSQL via `Npgsql` for production and SQLite for development/test.
- [ ] Database migrations run automatically on application startup via `context.Database.MigrateAsync()`.
- [ ] A seed migration inserts a default Swiss Chart of Accounts structure (account codes, names, and types).
- [ ] ASP.NET Core Identity is configured with cookie authentication; login page at `/login`, logout at `/logout`.
- [ ] Session sliding expiry is set to 30 minutes of inactivity.
- [ ] Three roles are seeded: Admin, Bookkeeper, Viewer.
- [ ] `MainLayout.razor` renders a MudBlazor persistent left-drawer nav menu and top app bar.
- [ ] Nav menu contains links to placeholder pages for: Dashboard, General Ledger, Invoicing, Bills, Reports, Margin Analysis, Tax, Budgets, Settings.
- [ ] Each placeholder page renders its module title and "Coming soon" text; all pages are protected with `[Authorize]`.
- [ ] `GET /health` endpoint returns HTTP 200 with `{ "status": "healthy" }`.
- [ ] Serilog is configured with console and rolling-file sinks; structured log output includes timestamp, level, and message.
- [ ] `Result<T>` type is implemented in `Kairn.Application` with `Ok(T)` and `Fail(string)` factory methods.
- [ ] `BaseEntity` with `Id` (Guid), `CreatedAt`, `UpdatedAt`, and `RowVersion` is implemented in `Kairn.Domain`.
- [ ] `AuditLogInterceptor` (SaveChanges interceptor) is registered and writes `AuditLog` records to the database.
- [ ] Fluxor is registered; a minimal `DashboardState` stub compiles without errors.
- [ ] MudBlazor `KairnTheme` is configured per `Kairn_ColorSystem.md`: light palette sets Primary = Lichen 500 `#3A9463`, Secondary = Slate 500 `#4D7A9E`, Background = Stone 50 `#F5F4F1`, Error = Signal 500 `#C2492A`, Warning = Summit 500 `#D4920F`, Success = Lichen 500 `#3A9463`, Info = Slate 500 `#4D7A9E`, DrawerBackground = Lichen 700 `#1F6040`, TextPrimary = Stone 900 `#1F1E1C`; font family is Inter.
- [ ] MudBlazor `KairnTheme` dark palette is also configured per `Kairn_ColorSystem.md`: Background = Stone 900 `#1F1E1C`, Surface = `#2C2B29`, DrawerBackground = Lichen 900 `#0D3322`, TextPrimary = Stone 50 `#F5F4F1`.
- [ ] A `ThemeService` allows toggling between light and dark mode; the selected mode is persisted in a `theme` cookie alongside the `lang` cookie.
- [ ] Tailwind CSS JIT build runs via `npm run build:css` inside `Kairn.Blazor`; output CSS is served as a static asset.
- [ ] `tailwind.config.js` includes all five Kairn color ramps (stone, lichen, slate, summit, signal) with their five tonal stops each, as defined in `Kairn_ColorSystem.md`.
- [ ] HTTPS is enforced; HSTS header is present in production.
- [ ] Application starts successfully with `dotnet run` (Development) and `docker compose up` (Production with PostgreSQL container).
- [ ] GitHub Actions workflow (`build-and-test.yml`) runs `dotnet build` and `dotnet test` on every push to any branch.
- [ ] At least one smoke test in `Kairn.Tests` asserts the DI container resolves without errors.
- [ ] `Microsoft.Extensions.Localization` is registered in `Program.cs` via `builder.Services.AddLocalization(o => o.ResourcesPath = "Resources")`.
- [ ] `RequestLocalizationMiddleware` is added to the pipeline with supported cultures `["en", "fr"]`, default culture `"en"`, and a `CookieRequestCultureProvider` listed first so the `lang` cookie takes precedence over browser headers.
- [ ] The `Resources/` folder hierarchy exists in `Kairn.Blazor` with sub-folders `Pages/`, `Shared/`, and `Components/` matching the component folder structure.
- [ ] Resource files for the shell exist in both languages: `Resources/Shared/MainLayout.en.resx` and `Resources/Shared/MainLayout.fr.resx`, `Resources/Shared/NavMenu.en.resx` and `Resources/Shared/NavMenu.fr.resx`.
- [ ] All nav menu labels and the app bar title are looked up via `IStringLocalizer`; no hard-coded English strings remain in `NavMenu.razor` or `MainLayout.razor`.
- [ ] A `LanguageSwitcher.razor` component in the top app bar shows the current language and a toggle button ("EN" / "FR"); clicking it sets the `lang` cookie and reloads the page to apply the new culture.
- [ ] The login page (`/login`) is fully translated in both languages via `Resources/Pages/Login.en.resx` and `Resources/Pages/Login.fr.resx`.
- [ ] Each placeholder module page title is localised (e.g. "General Ledger" / "Grand livre") via its own resource pair.
- [ ] Number and date formatting is applied globally via the active `CultureInfo`; a helper `CultureHelper` utility class provides locale-aware formatting for currency amounts and dates.
- [ ] A `LocalisationSmokeTest` in `Kairn.Tests` asserts that switching the culture to `fr` returns French strings from `IStringLocalizer<NavMenu>`.

## Notes

- Use `dotnet new sln` and `dotnet new classlib` / `blazorserver` templates to bootstrap.
- Connection strings must be read from environment variables; never committed to source control.
- The `appsettings.json` committed to source control must not contain real credentials — use `appsettings.Development.json` (git-ignored) or user secrets locally.
- Multi-tenancy groundwork: all root entities must include a `TenantId` column with a global EF Core query filter (SDD §6.4).

## Priority
Must Have

## Related Requirements
SRS NFR-M-01, NFR-M-02, NFR-M-03, NFR-M-04, NFR-S-01, NFR-S-02, NFR-S-03, NFR-S-05, NFR-S-06, NFR-U-03
SDD §2, §3, §5.1, §5.2, §8, §10.1, §10.2
