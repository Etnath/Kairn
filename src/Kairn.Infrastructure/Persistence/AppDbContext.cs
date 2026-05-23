using Kairn.Domain.Common;
using Kairn.Domain.Entities;
using Kairn.Infrastructure.Identity;
using Kairn.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly AuditLogInterceptor? _auditInterceptor;

    public AppDbContext(DbContextOptions<AppDbContext> options, AuditLogInterceptor? auditInterceptor = null)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<ReconciliationSession> ReconciliationSessions => Set<ReconciliationSession>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();
    public DbSet<ReconciliationMatch> ReconciliationMatches => Set<ReconciliationMatch>();
    public DbSet<RecurringEntry> RecurringEntries => Set<RecurringEntry>();
    public DbSet<RecurringEntryLine> RecurringEntryLines => Set<RecurringEntryLine>();
    public DbSet<RecurringJobLog> RecurringJobLogs => Set<RecurringJobLog>();
    public DbSet<CurrencyRate> CurrencyRates => Set<CurrencyRate>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillLine> BillLines => Set<BillLine>();
    public DbSet<BillAttachment> BillAttachments => Set<BillAttachment>();
    public DbSet<BillPayment> BillPayments => Set<BillPayment>();
    public DbSet<TenantApSettings> TenantApSettings => Set<TenantApSettings>();
    public DbSet<TenantDashboardSettings>    TenantDashboardSettings    => Set<TenantDashboardSettings>();
    public DbSet<UserDashboardPreferences>   UserDashboardPreferences   => Set<UserDashboardPreferences>();
    public DbSet<ProductLine>                ProductLines               => Set<ProductLine>();
    public DbSet<ProductLineAccount>         ProductLineAccounts        => Set<ProductLineAccount>();
    public DbSet<MarginAlert>                MarginAlerts               => Set<MarginAlert>();
    public DbSet<TaxRate>                    TaxRates                   => Set<TaxRate>();
    public DbSet<ExpenseReport> ExpenseReports => Set<ExpenseReport>();
    public DbSet<ExpenseReportLine> ExpenseReportLines => Set<ExpenseReportLine>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<FiscalYearClose> FiscalYearCloses => Set<FiscalYearClose>();
    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<DepreciationLog> DepreciationLogs => Set<DepreciationLog>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();
    public DbSet<InvoiceReminder> InvoiceReminders => Set<InvoiceReminder>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_auditInterceptor is not null)
            optionsBuilder.AddInterceptors(_auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global decimal precision: numeric(18,4)
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(4);
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // RowVersion is intended for PostgreSQL's xmin concurrency token.
        // SQLite stores it as a plain INTEGER that never auto-updates, so using it
        // as a concurrency token there causes DbUpdateConcurrencyException on every save.
        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<uint>(nameof(BaseEntity.RowVersion))
                    .IsConcurrencyToken(false);
            }
        }
    }
}
