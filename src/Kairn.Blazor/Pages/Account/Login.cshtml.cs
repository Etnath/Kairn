using Kairn.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace Kairn.Blazor.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IStringLocalizer<LoginModel> _localizer;

    public LoginModel(SignInManager<ApplicationUser> signInManager, IStringLocalizer<LoginModel> localizer)
    {
        _signInManager = signInManager;
        _localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string? culture)
    {
        // Handle inline language switch from the login page links
        if (!string.IsNullOrEmpty(culture))
        {
            Response.Cookies.Append(
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(
                    new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), SameSite = SameSiteMode.Lax });
        }

        ViewData["Localizer"] = _localizer;
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Localizer"] = _localizer;

        if (!ModelState.IsValid)
        {
            ErrorMessage = _localizer["Login.InvalidCredentials"];
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var url = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";
            return LocalRedirect(url);
        }

        ErrorMessage = result.IsLockedOut
            ? _localizer["Login.LockedOut"]
            : _localizer["Login.InvalidCredentials"];

        return Page();
    }
}
