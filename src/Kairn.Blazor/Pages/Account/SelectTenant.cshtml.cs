using Kairn.Application.Features.Tenants;
using Kairn.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Kairn.Blazor.Pages.Account;

public class SelectTenantModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITenantMembershipService membershipService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid TenantId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToPage("/Account/Login");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Forbid();

        if (!await membershipService.IsMemberAsync(userId, TenantId))
            return Forbid();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Forbid();

        user.ActiveTenantId = TenantId;
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);

        var url = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/";
        return LocalRedirect(url);
    }
}
