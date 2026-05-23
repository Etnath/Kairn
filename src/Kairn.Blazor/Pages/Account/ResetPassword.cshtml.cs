using Kairn.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kairn.Blazor.Pages.Account;

public class ResetPasswordModel(
    UserManager<ApplicationUser> userManager,
    IStringLocalizer<ResetPasswordModel> localizer) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        public string Token { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = "";
    }

    public IActionResult OnGet(string? token, string? email)
    {
        ViewData["Localizer"] = localizer;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            return RedirectToPage("Login");

        // Decode the URL-safe token back to the original token string
        try
        {
            Input.Token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch
        {
            return RedirectToPage("Login");
        }

        Input.Email = email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Localizer"] = localizer;

        if (!ModelState.IsValid)
        {
            ErrorMessage = localizer["Error.Validation"];
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null)
            return Redirect("/account/reset-password-confirmation"); // No enumeration

        var result = await userManager.ResetPasswordAsync(user, Input.Token, Input.Password);

        if (result.Succeeded)
            return Redirect("/account/reset-password-confirmation");

        // Token expired or already used
        ErrorMessage = localizer["Error.InvalidToken"];
        return Page();
    }
}
