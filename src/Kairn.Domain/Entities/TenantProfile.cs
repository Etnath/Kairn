namespace Kairn.Domain.Entities;

public enum BusinessStatus { Standard, AutoEntrepreneur }
public enum ActivityType   { Services, Commercial }

public class TenantProfile
{
    public Guid           TenantId              { get; set; }
    public string         LegalName             { get; set; } = "";
    public string         Siret                 { get; set; } = "";
    public string         AddressLine           { get; set; } = "";
    public string         PostalCode            { get; set; } = "";
    public string         City                  { get; set; } = "";
    public string         Country               { get; set; } = "France";
    public BusinessStatus BusinessStatus        { get; set; } = BusinessStatus.Standard;
    public ActivityType   ActivityType          { get; set; } = ActivityType.Services;
    public decimal?       VatThresholdServices  { get; set; }
    public decimal?       VatThresholdCommercial { get; set; }
    public string?        LogoPath              { get; set; }
}
