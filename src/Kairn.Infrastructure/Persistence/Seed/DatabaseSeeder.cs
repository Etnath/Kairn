using Kairn.Domain.Entities;
using Kairn.Infrastructure.Identity;
using Kairn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager, bool isDevelopment = false)
    {
        await SeedRolesAsync(roleManager);
        await SeedChartOfAccountsAsync(context);

        if (isDevelopment)
            await SeedDevAdminAsync(userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Admin", "Bookkeeper", "Viewer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedDevAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string email = "admin@kairn.local";
        const string password = "Admin1234!";

        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Admin",
            TenantId = Guid.Empty,
            PreferredLanguage = "fr",
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, "Admin");
    }

    private static async Task SeedChartOfAccountsAsync(AppDbContext context)
    {
        if (await context.Accounts.AnyAsync())
            return;

        var tenantId = Guid.Empty; // default tenant for seed
        var now = DateTimeOffset.UtcNow;

        var accounts = new List<Account>
        {
            // ── Assets (1xxx) ──────────────────────────────────────────────
            Acct("1000", "Cash / Petty Cash",                  AccountType.Asset,   tenantId, now, isCurrent: true),
            Acct("1020", "Bank Account CHF",                   AccountType.Asset,   tenantId, now, isCurrent: true),
            Acct("1021", "Bank Account EUR",                   AccountType.Asset,   tenantId, now, isCurrent: true),
            Acct("1100", "Accounts Receivable",                AccountType.Asset,   tenantId, now, isCurrent: true),
            Acct("1170", "VAT Input Tax Recoverable",          AccountType.Asset,   tenantId, now, isCurrent: true),
            Acct("1200", "Prepaid Expenses",                   AccountType.Asset,   tenantId, now, isCurrent: true),
            Acct("1500", "Inventory / Merchandise",            AccountType.Asset,   tenantId, now, isCurrent: true),
            Acct("1510", "Raw Materials",                      AccountType.Asset,   tenantId, now, isCurrent: true),
            // Fixed Assets (non-current)
            Acct("1700", "Machinery & Equipment",              AccountType.Asset,   tenantId, now, isCurrent: false),
            Acct("1710", "Accum. Depreciation – Machinery",   AccountType.Asset,   tenantId, now, isCurrent: false),
            Acct("1740", "Vehicles",                           AccountType.Asset,   tenantId, now, isCurrent: false),
            Acct("1741", "Accum. Depreciation – Vehicles",    AccountType.Asset,   tenantId, now, isCurrent: false),
            Acct("1770", "IT Equipment",                       AccountType.Asset,   tenantId, now, isCurrent: false),
            Acct("1771", "Accum. Depreciation – IT",          AccountType.Asset,   tenantId, now, isCurrent: false),
            Acct("1800", "Land",                               AccountType.Asset,   tenantId, now, isCurrent: false),
            Acct("1840", "Buildings",                          AccountType.Asset,   tenantId, now, isCurrent: false),
            Acct("1841", "Accum. Depreciation – Buildings",   AccountType.Asset,   tenantId, now, isCurrent: false),

            // ── Liabilities (2xxx) ────────────────────────────────────────
            Acct("2000", "Accounts Payable",                   AccountType.Liability, tenantId, now, isCurrent: true),
            Acct("2100", "Bank Loan (Short-Term)",             AccountType.Liability, tenantId, now, isCurrent: true),
            Acct("2200", "VAT Payable",                        AccountType.Liability, tenantId, now, isCurrent: true),
            Acct("2270", "Social Security Payable (AVS/AI)",   AccountType.Liability, tenantId, now, isCurrent: true),
            Acct("2300", "Accrued Liabilities",                AccountType.Liability, tenantId, now, isCurrent: true),
            Acct("2400", "Deferred Revenue",                   AccountType.Liability, tenantId, now, isCurrent: true),
            Acct("2500", "Bank Loan (Long-Term)",              AccountType.Liability, tenantId, now, isCurrent: false),

            // ── Equity (28xx–29xx) ────────────────────────────────────────
            Acct("2800", "Share Capital",                      AccountType.Equity, tenantId, now),
            Acct("2850", "Legal Reserves",                     AccountType.Equity, tenantId, now),
            Acct("2900", "Retained Earnings",                  AccountType.Equity, tenantId, now),
            Acct("2950", "Current Year Earnings",              AccountType.Equity, tenantId, now),
            Acct("2960", "Owner Drawings",                     AccountType.Equity, tenantId, now),

            // ── Revenue (3xxx) ────────────────────────────────────────────
            Acct("3000", "Sales Revenue – Products",           AccountType.Revenue, tenantId, now),
            Acct("3200", "Sales Revenue – Services",           AccountType.Revenue, tenantId, now),
            Acct("3400", "Other Operating Revenue",            AccountType.Revenue, tenantId, now),
            Acct("3800", "Financial Income",                   AccountType.Revenue, tenantId, now),
            Acct("3900", "Extraordinary Income",               AccountType.Revenue, tenantId, now),

            // ── Cost of Goods Sold (4xxx) ─────────────────────────────────
            Acct("4000", "Cost of Goods Sold",                 AccountType.Expense, tenantId, now),
            Acct("4200", "Direct Labour",                      AccountType.Expense, tenantId, now),
            Acct("4400", "Subcontractors",                     AccountType.Expense, tenantId, now),

            // ── Operating Expenses (5xxx–6xxx) ────────────────────────────
            Acct("5000", "Wages & Salaries",                   AccountType.Expense, tenantId, now),
            Acct("5700", "Social Insurance Contributions",     AccountType.Expense, tenantId, now),
            Acct("5800", "Staff Training & Development",       AccountType.Expense, tenantId, now),
            Acct("6000", "Rent",                               AccountType.Expense, tenantId, now),
            Acct("6100", "Utilities (Electricity, Water, Gas)",AccountType.Expense, tenantId, now),
            Acct("6200", "Office Supplies",                    AccountType.Expense, tenantId, now),
            Acct("6300", "IT & Software",                      AccountType.Expense, tenantId, now),
            Acct("6400", "Travel & Entertainment",             AccountType.Expense, tenantId, now),
            Acct("6500", "Marketing & Advertising",            AccountType.Expense, tenantId, now),
            Acct("6600", "Professional Services (Fees)",       AccountType.Expense, tenantId, now),
            Acct("6700", "Insurance",                          AccountType.Expense, tenantId, now),
            Acct("6800", "Depreciation & Amortisation",        AccountType.Expense, tenantId, now),
            Acct("6900", "Other Operating Expenses",           AccountType.Expense, tenantId, now),

            // ── Financial Expenses (7xxx) ─────────────────────────────────
            Acct("7500", "Interest Expense",                   AccountType.Expense, tenantId, now),
            Acct("7600", "Bank Charges",                       AccountType.Expense, tenantId, now),
            Acct("7700", "Foreign Exchange Loss",              AccountType.Expense, tenantId, now),

            // ── Tax (8xxx) ────────────────────────────────────────────────
            Acct("8000", "Income Tax Expense",                 AccountType.Expense, tenantId, now),
        };

        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();
    }

    private static Account Acct(string code, string name, AccountType type, Guid tenantId,
        DateTimeOffset now, bool isCurrent = true) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Code = code,
        Name = name,
        Type = type,
        Currency = "CHF",
        IsActive = true,
        IsCurrent = isCurrent,
        CreatedAt = now,
        UpdatedAt = now,
    };
}
