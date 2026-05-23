using Microsoft.AspNetCore.Identity;

namespace Kairn.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid? ActiveTenantId { get; set; }
    public string? DisplayName { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public bool IsDarkMode { get; set; }
}
