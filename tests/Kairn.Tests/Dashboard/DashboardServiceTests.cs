using FluentAssertions;
using Kairn.Domain.Entities;
using Kairn.Infrastructure.Persistence;
using Kairn.Infrastructure.Persistence.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kairn.Tests.Dashboard;

/// <summary>
/// Integration tests for DashboardService.GetKpisAsync.
///
/// Regression coverage for the Fluxor DI bug: DashboardEffects (singleton) could not
/// consume IDashboardService (previously scoped), so the effect was silently dropped and
/// GetKpisAsync was never called. The fix makes DashboardService singleton by having it
/// own its DbContext lifetime via IDbContextFactory (DashboardDbContextFactory).
/// These tests verify the service both resolves and returns correct data.
/// </summary>
public sealed class DashboardServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _seedDb = null!;
    private DashboardService _sut = null!;

    private readonly Guid _tenantId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        // Keep the connection open so the in-memory SQLite DB persists across contexts.
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var opts = BuildOptions();
        _seedDb = new AppDbContext(opts);
        await _seedDb.Database.EnsureCreatedAsync();

        _sut = new DashboardService(new KeepAliveDbContextFactory(_connection));
    }

    public async Task DisposeAsync()
    {
        await _seedDb.DisposeAsync();
        await _connection.DisposeAsync();
    }

    // ── Revenue & profit ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetKpisAsync_WithCurrentMonthRevenue_ReturnsCorrectAmounts()
    {
        var revenueAccount = await SeedAccountAsync(AccountType.Revenue, "7000");
        await SeedJournalLineAsync(revenueAccount.Id, debit: 0m, credit: 1_500m, date: Today());

        var kpis = await _sut.GetKpisAsync(_tenantId);

        kpis.MonthlyRevenue.Current.Should().Be(1_500m);
        kpis.NetProfit.Current.Should().Be(1_500m);
        kpis.MonthlyExpenses.Current.Should().Be(0m);
    }

    [Fact]
    public async Task GetKpisAsync_WithCurrentMonthExpense_ReturnsCorrectAmounts()
    {
        var expenseAccount = await SeedAccountAsync(AccountType.Expense, "6100");
        await SeedJournalLineAsync(expenseAccount.Id, debit: 800m, credit: 0m, date: Today());

        var kpis = await _sut.GetKpisAsync(_tenantId);

        kpis.MonthlyExpenses.Current.Should().Be(800m);
        kpis.NetProfit.Current.Should().Be(-800m);
        kpis.MonthlyRevenue.Current.Should().Be(0m);
    }

    [Fact]
    public async Task GetKpisAsync_PreviousMonthEntry_DoesNotCountInCurrentKpis()
    {
        var revenueAccount = await SeedAccountAsync(AccountType.Revenue, "7000");
        var lastMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);
        await SeedJournalLineAsync(revenueAccount.Id, debit: 0m, credit: 5_000m, date: lastMonth);

        var kpis = await _sut.GetKpisAsync(_tenantId);

        kpis.MonthlyRevenue.Current.Should().Be(0m,
            because: "the entry is from last month and must not appear in current KPIs");
        kpis.MonthlyRevenue.DeltaPct.Should().Be(-100m,
            because: "revenue dropped from 5000 to 0, which is a -100% change");
    }

    [Fact]
    public async Task GetKpisAsync_PreviousMonthRevenue_AppearsAsPriorForDelta()
    {
        var revenueAccount = await SeedAccountAsync(AccountType.Revenue, "7000");
        var lastMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);
        await SeedJournalLineAsync(revenueAccount.Id, debit: 0m, credit: 2_000m, date: lastMonth);

        var kpis = await _sut.GetKpisAsync(_tenantId);

        kpis.MonthlyRevenue.Prior.Should().Be(2_000m);
    }

    // ── Tenant isolation ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetKpisAsync_DataBelongsToOtherTenant_ReturnsZeros()
    {
        var otherTenant = Guid.NewGuid();
        var account = new Account
        {
            TenantId = otherTenant,
            Name = "Sales",
            Code = "7000",
            Type = AccountType.Revenue,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _seedDb.Accounts.Add(account);
        await _seedDb.SaveChangesAsync();

        var kpis = await _sut.GetKpisAsync(_tenantId);

        kpis.MonthlyRevenue.Current.Should().Be(0m);
        kpis.NetProfit.Current.Should().Be(0m);
    }

    // ── Empty state ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetKpisAsync_EmptyDatabase_ReturnsAllZeros()
    {
        var kpis = await _sut.GetKpisAsync(_tenantId);

        kpis.MonthlyRevenue.Current.Should().Be(0m);
        kpis.MonthlyExpenses.Current.Should().Be(0m);
        kpis.NetProfit.Current.Should().Be(0m);
        kpis.OutstandingAr.Current.Should().Be(0m);
        kpis.OutstandingAp.Current.Should().Be(0m);
        kpis.CashBalance.Current.Should().Be(0m);
    }

    // ── Exchange rate ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetKpisAsync_WithForeignCurrencyEntry_AppliesExchangeRate()
    {
        var account = await SeedAccountAsync(AccountType.Revenue, "7000");
        await SeedJournalLineAsync(account.Id, debit: 0m, credit: 1_000m, date: Today(), exchangeRate: 1.1m);

        var kpis = await _sut.GetKpisAsync(_tenantId);

        kpis.MonthlyRevenue.Current.Should().Be(1_100m);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<Account> SeedAccountAsync(AccountType type, string code)
    {
        var account = new Account
        {
            TenantId  = _tenantId,
            Name      = code,
            Code      = code,
            Type      = type,
            IsActive  = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _seedDb.Accounts.Add(account);
        await _seedDb.SaveChangesAsync();
        return account;
    }

    private async Task SeedJournalLineAsync(
        Guid accountId, decimal debit, decimal credit, DateOnly date, decimal exchangeRate = 1m)
    {
        var entry = new JournalEntry
        {
            TenantId  = _tenantId,
            Date      = date,
            Reference = "TEST",
            Description = "Test entry",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _seedDb.JournalEntries.Add(entry);
        await _seedDb.SaveChangesAsync();

        _seedDb.JournalLines.Add(new JournalLine
        {
            EntryId      = entry.Id,
            AccountId    = accountId,
            Debit        = debit,
            Credit       = credit,
            ExchangeRate = exchangeRate,
        });
        await _seedDb.SaveChangesAsync();
    }

    // Shares the open connection so every AppDbContext sees the same in-memory DB.
    private sealed class KeepAliveDbContextFactory(SqliteConnection connection)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
            => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default)
            => Task.FromResult(CreateDbContext());
    }

    private DbContextOptions<AppDbContext> BuildOptions()
        => new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
}
