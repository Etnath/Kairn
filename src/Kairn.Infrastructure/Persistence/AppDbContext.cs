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
    }
}
