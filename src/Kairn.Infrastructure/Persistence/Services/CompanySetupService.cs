using Kairn.Application.Features.Tenants;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class CompanySetupService(AppDbContext db) : ICompanySetupService
{
    private const decimal DefaultThresholdServices   = 77_700m;
    private const decimal DefaultThresholdCommercial = 188_700m;

    public async Task<Guid> CreateAsync(CreateCompanyCommand cmd, CancellationToken ct = default)
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 1. Tenant record
        var tenant = new Tenant { Id = tenantId, Name = cmd.CompanyName.Trim(), CreatedAt = now };
        db.Tenants.Add(tenant);

        // 2. TenantProfile
        db.TenantProfiles.Add(new TenantProfile
        {
            TenantId              = tenantId,
            LegalName             = cmd.CompanyName.Trim(),
            LegalForm             = cmd.LegalForm,
            Siret                 = cmd.Siren ?? "",
            AddressLine           = cmd.AddressLine?.Trim() ?? "",
            PostalCode            = cmd.PostalCode?.Trim() ?? "",
            City                  = cmd.City?.Trim() ?? "",
            Country               = "France",
            BusinessStatus        = cmd.BusinessStatus,
            ActivityType          = cmd.ActivityType,
            VatThresholdServices  = DefaultThresholdServices,
            VatThresholdCommercial = DefaultThresholdCommercial,
            FiscalYearStartMonth  = cmd.FiscalYearStartMonth,
            VatFilingFrequency    = cmd.VatFilingFrequency,
        });

        // 3. Chart of accounts
        var accounts = cmd.BusinessStatus == BusinessStatus.AutoEntrepreneur
            ? BuildAeAccounts(tenantId, now)
            : BuildStandardAccounts(tenantId, now);
        db.Accounts.AddRange(accounts);

        // 4. Tax rates
        db.TaxRates.AddRange(BuildTaxRates(tenantId, now));

        // 5. Membership (owner)
        db.TenantMemberships.Add(new TenantMembership
        {
            Tenant   = tenant,
            UserId   = cmd.UserId,
            Role     = TenantRole.Owner,
            JoinedAt = now,
        });

        await db.SaveChangesAsync(ct);
        return tenantId;
    }

    // ── COA builders ────────────────────────────────────────────────────────

    private static IEnumerable<Account> BuildStandardAccounts(Guid tenantId, DateTimeOffset now) =>
    [
        // Classe 1 – Capitaux
        Acct("101000", "Capital social",                                 AccountType.Equity,    tenantId, now),
        Acct("106100", "Réserve légale",                                 AccountType.Equity,    tenantId, now),
        Acct("106800", "Autres réserves",                                AccountType.Equity,    tenantId, now),
        Acct("108000", "Compte de l'exploitant",                         AccountType.Equity,    tenantId, now),
        Acct("110000", "Report à nouveau (solde créditeur)",             AccountType.Equity,    tenantId, now),
        Acct("119000", "Report à nouveau (solde débiteur)",              AccountType.Equity,    tenantId, now),
        Acct("120000", "Résultat de l'exercice (bénéfice)",              AccountType.Equity,    tenantId, now),
        Acct("129000", "Résultat de l'exercice (perte)",                 AccountType.Equity,    tenantId, now),
        Acct("164000", "Emprunts auprès des établissements de crédit",   AccountType.Liability, tenantId, now, isCurrent: false),
        Acct("168000", "Autres emprunts et dettes assimilées",           AccountType.Liability, tenantId, now, isCurrent: false),
        // Classe 2 – Immobilisations
        Acct("211000", "Terrains",                                       AccountType.Asset,     tenantId, now, isCurrent: false),
        Acct("213100", "Constructions – bâtiments industriels",          AccountType.Asset,     tenantId, now, isCurrent: false),
        Acct("215400", "Matériel industriel",                            AccountType.Asset,     tenantId, now, isCurrent: false),
        Acct("218200", "Matériel de transport",                          AccountType.Asset,     tenantId, now, isCurrent: false),
        Acct("218300", "Matériel de bureau et informatique",             AccountType.Asset,     tenantId, now, isCurrent: false),
        Acct("281310", "Amortissements – constructions",                 AccountType.Asset,     tenantId, now, isCurrent: false),
        Acct("281540", "Amortissements – matériel industriel",           AccountType.Asset,     tenantId, now, isCurrent: false),
        Acct("281820", "Amortissements – matériel de transport",         AccountType.Asset,     tenantId, now, isCurrent: false),
        Acct("281830", "Amortissements – matériel informatique",         AccountType.Asset,     tenantId, now, isCurrent: false),
        // Classe 3 – Stocks
        Acct("370000", "Stocks de marchandises",                         AccountType.Asset,     tenantId, now, isCurrent: true),
        Acct("371000", "Stocks de matières premières",                   AccountType.Asset,     tenantId, now, isCurrent: true),
        // Classe 4 – Tiers
        Acct("411000", "Clients",                                        AccountType.Asset,     tenantId, now, isCurrent: true),
        Acct("416000", "Clients douteux ou litigieux",                   AccountType.Asset,     tenantId, now, isCurrent: true),
        Acct("445660", "TVA déductible sur autres biens et services",    AccountType.Asset,     tenantId, now, isCurrent: true),
        Acct("486000", "Charges constatées d'avance",                    AccountType.Asset,     tenantId, now, isCurrent: true),
        Acct("401000", "Fournisseurs",                                   AccountType.Liability, tenantId, now, isCurrent: true),
        Acct("419000", "Clients créditeurs – avances et acomptes",       AccountType.Liability, tenantId, now, isCurrent: true),
        Acct("431000", "Sécurité sociale",                               AccountType.Liability, tenantId, now, isCurrent: true),
        Acct("437000", "Autres organismes sociaux",                      AccountType.Liability, tenantId, now, isCurrent: true),
        Acct("444000", "État – Impôts sur les bénéfices",                AccountType.Liability, tenantId, now, isCurrent: true),
        Acct("445710", "TVA collectée",                                  AccountType.Liability, tenantId, now, isCurrent: true),
        Acct("445820", "TVA à décaisser",                                AccountType.Liability, tenantId, now, isCurrent: true),
        Acct("487000", "Produits constatés d'avance",                    AccountType.Liability, tenantId, now, isCurrent: true),
        // Classe 5 – Comptes financiers
        Acct("512000", "Banque – compte principal",                      AccountType.Asset,     tenantId, now, isCurrent: true),
        Acct("512100", "Banque – compte secondaire",                     AccountType.Asset,     tenantId, now, isCurrent: true),
        Acct("530000", "Caisse",                                         AccountType.Asset,     tenantId, now, isCurrent: true),
        Acct("519000", "Concours bancaires courants",                    AccountType.Liability, tenantId, now, isCurrent: true),
        // Classe 6 – Charges
        Acct("601000", "Achats de matières premières",                   AccountType.Expense,   tenantId, now),
        Acct("607000", "Achats de marchandises",                         AccountType.Expense,   tenantId, now),
        Acct("611000", "Sous-traitance générale",                        AccountType.Expense,   tenantId, now),
        Acct("613000", "Locations",                                      AccountType.Expense,   tenantId, now),
        Acct("615000", "Entretien et réparations",                       AccountType.Expense,   tenantId, now),
        Acct("616000", "Primes d'assurances",                            AccountType.Expense,   tenantId, now),
        Acct("622600", "Honoraires",                                     AccountType.Expense,   tenantId, now),
        Acct("623000", "Publicité, publications, relations publiques",   AccountType.Expense,   tenantId, now),
        Acct("625100", "Voyages et déplacements",                        AccountType.Expense,   tenantId, now),
        Acct("625700", "Réceptions",                                     AccountType.Expense,   tenantId, now),
        Acct("626000", "Frais postaux et de télécommunications",         AccountType.Expense,   tenantId, now),
        Acct("627000", "Services bancaires et assimilés",                AccountType.Expense,   tenantId, now),
        Acct("641000", "Rémunérations du personnel",                     AccountType.Expense,   tenantId, now),
        Acct("645000", "Charges de sécurité sociale et de prévoyance",   AccountType.Expense,   tenantId, now),
        Acct("661100", "Intérêts des emprunts et dettes",                AccountType.Expense,   tenantId, now),
        Acct("681100", "Dotations aux amortissements – immo. corporelles", AccountType.Expense, tenantId, now),
        Acct("695000", "Impôts sur les bénéfices",                       AccountType.Expense,   tenantId, now),
        // Classe 7 – Produits
        Acct("701000", "Ventes de produits finis",                       AccountType.Revenue,   tenantId, now),
        Acct("706000", "Prestations de services",                        AccountType.Revenue,   tenantId, now),
        Acct("707000", "Ventes de marchandises",                         AccountType.Revenue,   tenantId, now),
        Acct("740000", "Subventions d'exploitation",                     AccountType.Revenue,   tenantId, now),
        Acct("758000", "Produits divers de gestion courante",            AccountType.Revenue,   tenantId, now),
        Acct("761000", "Produits de participations",                     AccountType.Revenue,   tenantId, now),
        Acct("775000", "Produits des cessions d'éléments d'actif",       AccountType.Revenue,   tenantId, now),
    ];

    private static IEnumerable<Account> BuildAeAccounts(Guid tenantId, DateTimeOffset now) =>
    [
        // Classe 1 – Essentials only
        Acct("108000", "Compte de l'exploitant",                         AccountType.Equity,    tenantId, now),
        Acct("110000", "Report à nouveau (solde créditeur)",             AccountType.Equity,    tenantId, now),
        Acct("120000", "Résultat de l'exercice (bénéfice)",              AccountType.Equity,    tenantId, now),
        Acct("129000", "Résultat de l'exercice (perte)",                 AccountType.Equity,    tenantId, now),
        // Classe 4 – Tiers (no VAT accounts)
        Acct("411000", "Clients",                                        AccountType.Asset,     tenantId, now, isCurrent: true),
        Acct("401000", "Fournisseurs",                                   AccountType.Liability, tenantId, now, isCurrent: true),
        // Classe 5 – Comptes financiers
        Acct("512000", "Banque – compte principal",                      AccountType.Asset,     tenantId, now, isCurrent: true),
        Acct("530000", "Caisse",                                         AccountType.Asset,     tenantId, now, isCurrent: true),
        // Classe 6 – Charges courantes
        Acct("611000", "Sous-traitance générale",                        AccountType.Expense,   tenantId, now),
        Acct("613000", "Locations",                                      AccountType.Expense,   tenantId, now),
        Acct("615000", "Entretien et réparations",                       AccountType.Expense,   tenantId, now),
        Acct("616000", "Primes d'assurances",                            AccountType.Expense,   tenantId, now),
        Acct("622600", "Honoraires",                                     AccountType.Expense,   tenantId, now),
        Acct("625100", "Voyages et déplacements",                        AccountType.Expense,   tenantId, now),
        Acct("626000", "Frais postaux et de télécommunications",         AccountType.Expense,   tenantId, now),
        Acct("627000", "Services bancaires et assimilés",                AccountType.Expense,   tenantId, now),
        // Classe 7 – Recettes
        Acct("706000", "Prestations de services",                        AccountType.Revenue,   tenantId, now),
        Acct("707000", "Ventes de marchandises",                         AccountType.Revenue,   tenantId, now),
        Acct("758000", "Produits divers de gestion courante",            AccountType.Revenue,   tenantId, now),
    ];

    private static IEnumerable<TaxRate> BuildTaxRates(Guid tenantId, DateTimeOffset now) =>
    [
        TaxRt(tenantId, "TVA Normale",        20m,  TaxCategory.Standard,     isDefault: true,  new DateOnly(2014, 1, 1), now),
        TaxRt(tenantId, "TVA Intermédiaire",  10m,  TaxCategory.Intermediate, isDefault: true,  new DateOnly(2012, 1, 1), now),
        TaxRt(tenantId, "TVA Réduite",         5.5m, TaxCategory.Reduced,     isDefault: true,  new DateOnly(1982, 7, 1), now),
        TaxRt(tenantId, "TVA Super-Réduite",   2.1m, TaxCategory.SuperReduced, isDefault: true, new DateOnly(1982, 7, 1), now),
        TaxRt(tenantId, "TVA Exonérée",        0m,  TaxCategory.Exempt,       isDefault: true,  new DateOnly(2014, 1, 1), now),
    ];

    private static Account Acct(string code, string name, AccountType type, Guid tenantId,
        DateTimeOffset now, bool isCurrent = true) => new()
    {
        Id        = Guid.NewGuid(),
        TenantId  = tenantId,
        Code      = code,
        Name      = name,
        Type      = type,
        Currency  = "EUR",
        IsActive  = true,
        IsCurrent = isCurrent,
        CreatedAt = now,
        UpdatedAt = now,
    };

    private static TaxRate TaxRt(Guid tenantId, string name, decimal rate, TaxCategory category,
        bool isDefault, DateOnly validFrom, DateTimeOffset now) => new()
    {
        Id        = Guid.NewGuid(),
        TenantId  = tenantId,
        Name      = name,
        Rate      = rate,
        Category  = category,
        IsDefault = isDefault,
        ValidFrom = validFrom,
        ValidTo   = null,
        IsActive  = true,
        CreatedAt = now,
        UpdatedAt = now,
    };
}
