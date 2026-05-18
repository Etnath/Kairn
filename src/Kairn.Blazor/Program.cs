using Fluxor;
using Kairn.Application.Common;
using Kairn.Application.Features.AR;
using Kairn.Application.Features.Audit;
using Kairn.Application.Features.Reconciliation;
using Kairn.Application.Features.Reports;
using Kairn.Application.Features.GL;
using Kairn.Infrastructure.Email;
using Kairn.Infrastructure.Jobs;
using Kairn.Infrastructure.Reports;
using Kairn.Infrastructure.Identity;
using Kairn.Infrastructure.Persistence;
using Kairn.Infrastructure.Persistence.Interceptors;
using Kairn.Infrastructure.Persistence.Seed;
using Kairn.Infrastructure.Persistence.Services;
using Kairn.Blazor.Localisation;
using Kairn.Blazor.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;

// ── Serilog bootstrap ───────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/kairn-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

    // ── Database ─────────────────────────────────────────────────────────────
    builder.Services.AddScoped<AuditLogInterceptor>();
    builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
    builder.Services.AddHttpContextAccessor();

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=kairn.db")
                   .AddInterceptors(sp.GetRequiredService<AuditLogInterceptor>()));
    }
    else
    {
        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Default"),
                       npgsql => npgsql.EnableRetryOnFailure(3).CommandTimeout(60))
                   .AddInterceptors(sp.GetRequiredService<AuditLogInterceptor>()));
    }

    // ── Identity ─────────────────────────────────────────────────────────────
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    // ── Razor components + pages (for auth) ──────────────────────────────────
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddRazorPages();

    // ── MudBlazor ────────────────────────────────────────────────────────────
    builder.Services.AddSingleton<MudLocalizer, KairnMudLocalizer>();
    builder.Services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
        config.SnackbarConfiguration.ShowCloseIcon = true;
    });

    // ── Fluxor ───────────────────────────────────────────────────────────────
    builder.Services.AddFluxor(options =>
        options.ScanAssemblies(typeof(Program).Assembly));

    // ── Localisation ─────────────────────────────────────────────────────────
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

    // ── Application services ─────────────────────────────────────────────────
    builder.Services.AddScoped<ThemeService>();
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<IInvoiceService, InvoiceService>();
    builder.Services.AddScoped<IInvoicePaymentService, InvoicePaymentService>();
    builder.Services.AddScoped<IArAgingService, ArAgingService>();
    builder.Services.AddSingleton<IArAgingExporter, ArAgingExporter>();
    builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
    builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
    builder.Services.AddScoped<IReminderService, ReminderService>();
    builder.Services.AddScoped<IJournalEntryService, JournalEntryService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddScoped<IReconciliationService, ReconciliationService>();
    builder.Services.AddSingleton<IOfxParser, OfxParser>();
    builder.Services.AddSingleton<ICsvParser, CsvParser>();
    builder.Services.AddScoped<ITrialBalanceService, TrialBalanceService>();
    builder.Services.AddSingleton<ITrialBalanceExporter, TrialBalanceExporter>();
    builder.Services.AddScoped<IRecurringEntryService, RecurringEntryService>();
    builder.Services.AddHostedService<RecurringPostingJob>();
    builder.Services.AddHostedService<OverdueInvoiceJob>();
    builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
    builder.Services.AddHostedService<ExchangeRateRefreshJob>();
    builder.Services.AddHttpClient("Frankfurter", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    // ── Health checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Migrate + seed on startup ─────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db, userManager, roleManager, app.Environment.IsDevelopment());
    }

    // ── Localisation middleware ───────────────────────────────────────────────
    // Only honour the culture cookie (and optional query string). The browser's
    // Accept-Language header is intentionally ignored so the app always defaults
    // to French rather than inheriting whatever language the user's browser prefers.
    var supportedCultures = new[] { "fr-FR", "fr", "en" };
    var localizationOptions = new RequestLocalizationOptions()
        .SetDefaultCulture("fr-FR")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    localizationOptions.RequestCultureProviders =
    [
        new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider(),
        new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider(),
    ];
    app.UseRequestLocalization(localizationOptions);

    // ── Pipeline ──────────────────────────────────────────────────────────────
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapRazorComponents<Kairn.Blazor.App>()
        .AddInteractiveServerRenderMode();
    app.MapRazorPages();
    app.MapHealthChecks("/health");

    // PDF download endpoint
    app.MapGet("/api/invoices/{id:guid}/pdf", async (Guid id, IInvoiceService invoiceSvc, ICurrentUserContext user) =>
    {
        var bytes = await invoiceSvc.GeneratePdfAsync(id, user.TenantId);
        if (bytes is null) return Results.NotFound();
        return Results.File(bytes, "application/pdf", $"invoice-{id}.pdf");
    }).RequireAuthorization();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Expose Program for WebApplicationFactory in tests
public partial class Program { }
