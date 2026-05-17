# Kairn — Financial Management ERP
## Software Design Document — Blazor Web Implementation
**Version 1.0 · May 2026 · Draft**

---

| Field | Value |
|---|---|
| Product Name | Kairn — Financial Management ERP |
| Platform | Web (Blazor Server + optionally Blazor WebAssembly) |
| Version | 1.0 |
| Date | May 2026 |
| Status | Draft |

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Architectural Overview](#2-architectural-overview)
3. [Solution Structure](#3-solution-structure)
4. [Component Design Patterns](#4-component-design-patterns)
5. [Server-Side & API Design](#5-server-side--api-design)
6. [Data Layer Design](#6-data-layer-design)
7. [UI Design Guidelines](#7-ui-design-guidelines)
8. [Security Design](#8-security-design)
9. [Testing Strategy](#9-testing-strategy)
10. [Deployment & Infrastructure](#10-deployment--infrastructure)
11. [Risks & Mitigations](#11-risks--mitigations)

---

## 1. Introduction

### 1.1 Purpose

This Software Design Document (SDD) describes the architecture, design patterns, component breakdown, and technology choices for the Blazor web implementation of **Kairn**. It targets a cross-platform, browser-accessible deployment, making the system available from any device without installation.

### 1.2 Scope

This document covers the full Blazor application (client and server-side concerns), the shared domain and infrastructure layers, and the deployment strategy. It is intended for developers building and maintaining the application.

### 1.3 Blazor Hosting Model

The recommended hosting model is **Blazor Server**. The Razor component tree runs on the server, and UI events are transmitted over a persistent SignalR connection. This eliminates WebAssembly download concerns and allows direct access to server-side resources (database, file system). A future Blazor WebAssembly build for offline use is outlined in Section 10.

### 1.4 Technology Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 8 (LTS) |
| UI Framework | Blazor Server (ASP.NET Core hosted) |
| Component Model | Razor Components (.razor + C# code-behind) |
| State Management | Scoped services + Fluxor (Redux-style) for complex UI state |
| CSS Framework | Tailwind CSS (JIT) + custom design tokens |
| Component Library | MudBlazor (Material Design) |
| Charts | ApexCharts.Blazor |
| ORM | Entity Framework Core 8 |
| Database (Production) | PostgreSQL via Npgsql.EntityFrameworkCore |
| Database (Dev/Test) | SQLite |
| Authentication | ASP.NET Core Identity + Cookie authentication |
| Authorization | Policy-based with IAuthorizationService |
| PDF Generation | QuestPDF (server-side) |
| Excel Export | ClosedXML (server-side) |
| Real-time Updates | SignalR (built into Blazor Server) |
| Logging | Serilog with OpenTelemetry exporter |
| Testing | bUnit (components) + xUnit + Playwright (E2E) |
| Deployment | Docker container → any OCI-compatible host |

---

## 2. Architectural Overview

### 2.1 Layered Architecture

Kairn uses a strict four-layer architecture. Dependencies flow downward only — upper layers depend on lower ones, never the reverse.

| Layer | Description |
|---|---|
| **Presentation** | Blazor Razor components: Pages (routable), Shared layout, UI-only components. Zero business logic. Binds to application services via DI. |
| **Application** | Application services, DTOs, validators, Result\<T\> pattern. Injected into Blazor components via DI. |
| **Domain** | Core business entities, domain services, business rules (double-entry validation, depreciation, margin calculations). No framework dependencies. |
| **Infrastructure** | EF Core DbContext, Repository implementations, currency API client, PDF/Excel generators, email service, backup service. |
| **Hosting** | ASP.NET Core Program.cs: middleware pipeline, SignalR hub, authentication, HTTPS, static file serving, health checks. |

### 2.2 Request Lifecycle (Blazor Server)

1. Browser loads the initial HTML from the server (HTTP).
2. Blazor negotiates a persistent SignalR connection.
3. User interaction events (clicks, form input) are sent over SignalR to the server.
4. The server re-renders the affected component subtree and sends a compact diff back to the browser.
5. The browser applies the diff to the DOM. No full page reloads occur after initial load.

### 2.3 Shared Code Strategy

Domain, Application, and Infrastructure are class library projects referenced by the Blazor host. This makes the business logic fully portable — a future mobile or desktop client can reuse the same core without duplication.

```
Kairn.Domain          ← entities, value objects, domain services
Kairn.Application     ← use-case services, DTOs, validators
Kairn.Infrastructure  ← EF Core, repositories, external integrations
Kairn.Blazor          ← Blazor Server host (Presentation + ASP.NET Core)
Kairn.Tests           ← all test projects
```

---

## 3. Solution Structure

### 3.1 Project Layout

```
Kairn.sln
├── Kairn.Domain
├── Kairn.Application
├── Kairn.Infrastructure
├── Kairn.Blazor
│   ├── Pages/                  ← Routable page components (@page directive)
│   ├── Shared/                 ← MainLayout, NavMenu, AppBar
│   ├── Components/
│   │   ├── Finance/            ← KpiCard, LedgerGrid, InvoiceForm …
│   │   └── Charts/             ← RevenueChart, MarginChart …
│   ├── State/                  ← Fluxor stores (DashboardState, etc.)
│   ├── wwwroot/                ← Static assets, Tailwind output CSS
│   └── Program.cs              ← ASP.NET Core host, DI, middleware
└── Kairn.Tests
    ├── Domain.Tests
    ├── Application.Tests
    ├── Blazor.ComponentTests   ← bUnit component tests
    └── E2E.Tests               ← Playwright end-to-end tests
```

### 3.2 Component Inventory

| Component | Type | Responsibility |
|---|---|---|
| MainLayout.razor | Layout | Shell: left drawer nav, top app bar, main body |
| NavMenu.razor | Shared | Module navigation links with MudNavMenu |
| Dashboard.razor | Page | KPI cards, revenue chart, alerts panel |
| GeneralLedger.razor | Page | Journal entry table, filters, drill-down dialog |
| JournalEntryDialog.razor | Component | Modal for creating/editing journal entries with line items |
| Invoicing.razor | Page | Invoice list with status filters |
| InvoiceForm.razor | Component | Full invoice editor: customer, line items, totals |
| Reports.razor | Page | Report selector: P&L, Balance Sheet, Cash Flow |
| ProfitLossReport.razor | Component | Rendered P&L with period picker, drill-down, export |
| BalanceSheetReport.razor | Component | Balance sheet with comparative toggle |
| MarginAnalysis.razor | Page | Product line margin table + ApexChart trend |
| Settings.razor | Page | COA editor, tax rates, fiscal year, user management |
| KpiCard.razor | Component | Reusable KPI card: title, value, trend, icon |
| FinancialGrid.razor | Component | MudDataGrid wrapper: right-aligned decimals, row coloring |
| DateRangePicker.razor | Component | From/to pickers + preset buttons (This Month, YTD, etc.) |
| StatusBadge.razor | Component | Colored MudChip for invoice/bill status display |
| CurrencyInput.razor | Component | Numeric input with decimal precision and currency symbol |
| DashboardStore.cs | Fluxor State | Redux store for dashboard KPI data and refresh actions |
| LedgerStore.cs | Fluxor State | Store for active journal entry filter and results |

---

## 4. Component Design Patterns

### 4.1 Component Anatomy

Each Blazor page or component follows a standard structure: a `@page` directive (pages only), `@inject` statements for DI services, an HTML/Razor template block, and a `@code` section. Complex components use a code-behind file (`ComponentName.razor.cs`) to keep the `.razor` file readable.

```razor
@* ProfitLossReport.razor *@
@inject IReportingService ReportingService
@inject ISnackbar Snackbar

<MudCard>
    <MudCardHeader>
        <MudText Typo="Typo.h6">Profit & Loss Statement</MudText>
        <DateRangePicker @bind-Range="_dateRange" OnChanged="LoadReportAsync" />
    </MudCardHeader>
    <MudCardContent>
        @if (_loading) { <MudProgressLinear Indeterminate /> }
        else if (_report is not null)
        { <FinancialGrid Data="_report.Lines" /> }
    </MudCardContent>
</MudCard>

@code {
    private DateRange _dateRange = DateRange.CurrentMonth;
    private ProfitLossDto? _report;
    private bool _loading;

    private async Task LoadReportAsync()
    {
        _loading = true;
        StateHasChanged();
        var result = await ReportingService.GetProfitLossAsync(_dateRange);
        if (result.IsSuccess) _report = result.Value;
        else Snackbar.Add(result.Error, Severity.Error);
        _loading = false;
    }
}
```

### 4.2 State Management with Fluxor

Complex cross-component state (dashboard refresh, active filters) is managed with Fluxor. Each store module contains a State record, Actions (plain records), Reducers (pure functions), and Effects (async operations that call application services).

```csharp
// DashboardState.cs
[FeatureState]
public record DashboardState(bool IsLoading, DashboardKpiDto? Kpis, string? Error);

// Actions
public record LoadDashboardAction;
public record DashboardLoadedAction(DashboardKpiDto Kpis);

// Reducer
public static class DashboardReducers
{
    [ReducerMethod]
    public static DashboardState OnLoad(DashboardState s, LoadDashboardAction _)
        => s with { IsLoading = true };

    [ReducerMethod]
    public static DashboardState OnLoaded(DashboardState s, DashboardLoadedAction a)
        => s with { IsLoading = false, Kpis = a.Kpis };
}
```

### 4.3 Forms and Validation

All data-entry forms use Blazor's `EditForm` with `DataAnnotationsValidator` and a custom FluentValidation bridge. Validation runs on-change (real-time) for key fields. Server-side validation (double-entry balance check, duplicate reference detection) runs on submit and errors are fed back into the `ValidationMessageStore`.

```razor
<EditForm Model="_command" OnValidSubmit="HandleSubmitAsync">
    <DataAnnotationsValidator />
    <MudTextField @bind-Value="_command.Reference" Label="Reference"
                  For="@(() => _command.Reference)" />
    <JournalLineGrid @bind-Lines="_command.Lines"
                     OnBalanceChanged="UpdateBalance" />
    <MudText Color="@(_isBalanced ? Color.Success : Color.Error)">
        Balance: @_balance.ToString("N2")
    </MudText>
    <MudButton ButtonType="ButtonType.Submit" Disabled="!_isBalanced">Save</MudButton>
</EditForm>
```

### 4.4 Result Pattern

Application services return `Result<T>` (a discriminated union of success/error) so components can handle domain validation failures gracefully without exceptions.

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}

// Usage in a component
var result = await _journalService.CreateEntryAsync(command);
if (!result.IsSuccess)
    Snackbar.Add(result.Error, Severity.Error);
```

### 4.5 Double-Entry Validation (Domain Rule)

The `JournalEntry` domain aggregate enforces that debits equal credits before an entry can be committed. This is a domain-layer invariant, not delegated to the UI or infrastructure layers.

```csharp
// JournalEntry.cs (Domain)
public void Validate()
{
    var totalDebits  = Lines.Sum(l => l.Debit);
    var totalCredits = Lines.Sum(l => l.Credit);
    if (totalDebits != totalCredits)
        throw new DomainException(
            $"Journal entry unbalanced: debits {totalDebits} ≠ credits {totalCredits}");
}
```

---

## 5. Server-Side & API Design

### 5.1 Hosting Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddFluxor(o => o.ScanAssemblies(typeof(Program).Assembly));

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IJournalRepository, JournalRepository>();
builder.Services.AddScoped<IJournalEntryService, JournalEntryService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/login"; o.SlidingExpiration = true; });

builder.Services.AddAuthorizationCore(PolicyConfig.Configure);

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapHealthChecks("/health");
app.Run();
```

### 5.2 Authentication & Authorization

Authentication uses ASP.NET Core Identity with cookie sessions. Passwords are hashed with PBKDF2-SHA256 (Identity default). RBAC is implemented via Claims-based policies (Admin, Bookkeeper, Viewer). Components use `<AuthorizeView>` and `[Authorize(Policy="...")]` page attributes.

```razor
@* Protecting a page *@
@attribute [Authorize(Policy = "BookkeeperOrAdmin")]
@page "/ledger"

@* Inline conditional rendering *@
<AuthorizeView Policy="Admin">
    <Authorized>
        <MudButton OnClick="DeleteEntryAsync">Delete</MudButton>
    </Authorized>
</AuthorizeView>
```

### 5.3 Real-Time Notifications

Blazor Server's built-in SignalR connection is leveraged for real-time UI updates. A `NotificationHub` broadcasts events (invoice paid, budget threshold exceeded) to connected users. Components subscribe via a scoped `NotificationService` that wraps `IHubContext<NotificationHub>`.

---

## 6. Data Layer Design

### 6.1 PostgreSQL Configuration

PostgreSQL is the default database for server deployment. Decimal columns use `numeric(18,4)`. EF Core owned entities mapped to JSONB are used for flexible line-item storage.

```csharp
// AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Global decimal precision
    foreach (var property in modelBuilder.Model.GetEntityTypes()
        .SelectMany(e => e.GetProperties())
        .Where(p => p.ClrType == typeof(decimal)))
    {
        property.SetPrecision(18);
        property.SetScale(4);
    }

    // Soft-delete filter
    modelBuilder.Entity<JournalEntry>().HasQueryFilter(e => !e.IsDeleted);

    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}

protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    options.UseNpgsql(connectionString, o =>
    {
        o.EnableRetryOnFailure(3);
        o.CommandTimeout(60);
    });
}
```

### 6.2 Repository Pattern

All data access goes through typed repositories implementing generic interfaces. Application services depend on interfaces only; EF Core implementations are registered in the infrastructure layer.

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);  // soft delete
}

public interface IJournalRepository : IRepository<JournalEntry>
{
    Task<List<JournalEntry>> GetByDateRangeAsync(DateOnly from, DateOnly to);
    Task<TrialBalance> GetTrialBalanceAsync(DateOnly asOf);
}
```

### 6.3 Migration Strategy

Migrations are generated via `dotnet ef migrations add` and applied automatically on startup via `context.Database.MigrateAsync()`. Each migration includes both `Up` and `Down` methods and is source-controlled alongside the codebase. A seed migration populates the default Chart of Accounts on first run.

### 6.4 Multi-Tenancy Groundwork

All root entities include a `TenantId` column. A global EF Core query filter scopes all queries automatically. This enables future multi-tenant SaaS hosting with minimal schema changes.

### 6.5 Optimistic Concurrency

All entities include a `RowVersion` column (mapped to PostgreSQL's `xmin` system column). EF Core detects conflicts and throws `DbUpdateConcurrencyException`, which the application layer catches and returns as `Result.Fail("Record was modified by another user")`.

### 6.6 Audit Trail

A `SaveChangesInterceptor` intercepts every `SaveChangesAsync` call and writes an `AuditLog` record for each modified entity, capturing entity type, record ID, changed properties, old and new values, timestamp, and current user ID from a scoped `CurrentUserContext`.

---

## 7. UI Design Guidelines

### 7.1 Layout

`MainLayout.razor` uses `MudLayout` with a `MudDrawer` (persistent on desktop, temporary on mobile) containing the `NavMenu`, and a `MudAppBar` at the top. The main content area uses `MudMainContent` inside `MudScrollbar`. The layout is fully responsive using MudBlazor's breakpoint system.

### 7.2 Design Tokens

| Token | Value |
|---|---|
| Primary Color | `#1F3D2E` (deep forest) |
| Secondary Color | `#2E7D52` (kairn green) |
| Background | `#F4F7F5` |
| Surface (Cards) | `#FFFFFF` with MudBlazor Elevation 2 |
| Font Family | Inter (Google Fonts), fallback Segoe UI, Arial |
| Base Font Size | 0.875rem (14px) |
| Danger / Alert | `#C0392B` |
| Success | `#27AE60` |
| Table Stripe | `#EAF3EE` |
| Border Radius | 8px (cards), 4px (inputs) |

### 7.3 Custom Components

**KpiCard.razor** — `MudCard` containing an icon, title, large value, and trend chip (arrow icon + delta %).

**FinancialGrid.razor** — `MudDataGrid` with right-aligned decimal column templates, negative value coloring (red), and built-in export buttons (PDF/Excel).

**DateRangePicker.razor** — Two `MudDatePicker` components plus `MudButtonGroup` presets (This Month, Last Month, YTD, Last Year). Raises `OnChanged` on either end change.

**StatusBadge.razor** — `MudChip` with Color computed from status enum: Grey=Draft, Info=Sent, Error=Overdue, Success=Paid.

**CurrencyInput.razor** — `MudNumericField<decimal>` with configured decimal places, Adornment for currency symbol, and locale-aware formatting.

### 7.4 Progressive Loading

Pages use a two-phase render strategy: the initial render shows `MudSkeleton` loaders immediately, then data is loaded asynchronously and `StateHasChanged()` triggers a re-render with actual content. This prevents blank screens during database queries.

---

## 8. Security Design

| Concern | Approach |
|---|---|
| Authentication | ASP.NET Core Identity + cookie auth; PBKDF2-SHA256 password hashing |
| Session | Sliding cookie expiry (default 30 min idle); Secure + HttpOnly + SameSite=Strict |
| CSRF | Blazor Server is inherently protected — no cross-origin form posts; SignalR circuit is origin-locked |
| Database | PostgreSQL with TLS connection; `pg_hba.conf` restricts access by host |
| Transport | HTTPS enforced; HSTS header; TLS 1.2+ minimum via Kestrel config |
| RBAC | Claims policy — Admin, Bookkeeper, Viewer. Checked at page and component level. |
| Secrets | `appsettings.json` excluded from source control; connection strings via environment variables or a secrets manager |
| Audit Logging | EF Core interceptor logs all mutations to `AuditLog` table + Serilog structured logs |

---

## 9. Testing Strategy

### 9.1 Unit Tests (Domain + Application)

Domain entities and application services are tested with xUnit and Moq. Repositories are mocked. Tests cover: double-entry validation, depreciation calculations, VAT computation, aging bucket assignment, and margin ratio accuracy.

**Target: 80%+ line coverage on Domain and Application projects.**

### 9.2 Component Tests (bUnit)

bUnit enables Blazor component testing without a browser. Coverage includes:

- `KpiCard` renders correct value and trend color
- `DateRangePicker` emits correct dates on preset selection
- `FinancialGrid` renders negative values in red
- `JournalEntryDialog` disables Save when entry is unbalanced
- `AuthorizeView` hides delete buttons for Viewer role

```csharp
[Fact]
public void KpiCard_ShowsRedTrend_WhenDeltaIsNegative()
{
    using var ctx = new TestContext();
    var cut = ctx.RenderComponent<KpiCard>(p => p
        .Add(c => c.Title, "Revenue")
        .Add(c => c.Value, 45000m)
        .Add(c => c.Delta, -12.5m));

    cut.Find(".trend-chip").ClassList.Should().Contain("mud-error-text");
}
```

### 9.3 End-to-End Tests (Playwright)

Playwright tests run against a test instance with seeded data. Critical paths covered:

- Login and logout
- Creating a balanced and an unbalanced journal entry
- Generating a P&L report for a selected date range
- Creating and paying an invoice
- Exporting a report as PDF

---

## 10. Deployment & Infrastructure

### 10.1 Containerization

```dockerfile
# Dockerfile (multi-stage)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN npm --prefix Kairn.Blazor run build:css   # Tailwind JIT
RUN dotnet publish Kairn.Blazor -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Kairn.Blazor.dll"]
```

### 10.2 Deployment Options

| Target | Stack |
|---|---|
| Self-Hosted VPS | Docker Compose: app container + PostgreSQL + Nginx reverse proxy with Let's Encrypt TLS |
| Azure | App Service (container) + Azure Database for PostgreSQL Flexible Server + Key Vault |
| AWS | ECS Fargate + RDS PostgreSQL + Secrets Manager |
| Migrations | Applied automatically on startup via `context.Database.MigrateAsync()` |
| Backups | PostgreSQL `pg_dump` via scheduled cron; point-in-time recovery via WAL archiving |
| CI/CD | GitHub Actions: build → test → Docker build & push → rolling deploy |
| Health Check | `GET /health` integrated with Docker health check and load balancer |

### 10.3 Docker Compose (Self-Hosted)

```yaml
services:
  app:
    image: kairn:latest
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__Default=Host=db;Database=kairn;Username=kairn;Password=${DB_PASSWORD}
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      db:
        condition: service_healthy

  db:
    image: postgres:16
    environment:
      POSTGRES_DB: kairn
      POSTGRES_USER: kairn
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U kairn"]
      interval: 10s
      retries: 5

volumes:
  pgdata:
```

### 10.4 Future: Blazor WebAssembly (Offline Mode)

The application can be adapted to a Blazor WASM build for offline capability. In this model, the .NET runtime and application DLLs are downloaded to the browser. A local IndexedDB store caches data; a background sync service pushes changes to the server when connectivity is restored. This requires a separate REST or gRPC API layer and is planned as a Phase 2 enhancement.

---

## 11. Risks & Mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| SignalR connection drops on flaky networks | Medium | MudBlazor shows a reconnect overlay by default; configure Blazor reconnect intervals and max retries; implement circuit breaker pattern |
| Large datasets cause slow initial render | Medium | Server-side pagination on all grids; virtual scrolling via `MudDataGrid.Virtualize`; lazy-load charts |
| Browser tab memory growth over long sessions | Low | Dispose `IDisposable` components properly via `@implements IAsyncDisposable`; encourage periodic browser refresh |
| PostgreSQL connection pool exhaustion | Low | Configure Npgsql pool size; use scoped `DbContext` (one per SignalR circuit); monitor via `pg_stat_activity` |
| Tailwind CSS purge removes needed utility classes | Low | Configure `safelist` in `tailwind.config.js` for dynamic class names; use full CSS build in development |
| PostgreSQL unavailable during deploy | Low | EF Core retry-on-failure policy; health check gates traffic until migrations complete |

---

*Kairn — navigate your finances with confidence.*
