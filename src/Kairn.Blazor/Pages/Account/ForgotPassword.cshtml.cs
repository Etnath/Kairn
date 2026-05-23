using Kairn.Application.Features.AR;
using Kairn.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kairn.Blazor.Pages.Account;

public class ForgotPasswordModel(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IStringLocalizer<ForgotPasswordModel> localizer) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }

    public void OnGet() => ViewData["Localizer"] = localizer;

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Localizer"] = localizer;

        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);

        // Always redirect — no user enumeration
        if (user is not null && emailService.IsConfigured)
        {
            var token    = await userManager.GeneratePasswordResetTokenAsync(user);
            var encoded  = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var baseUrl  = $"{Request.Scheme}://{Request.Host}";
            var resetUrl = $"{baseUrl}/account/reset-password?token={encoded}&email={Uri.EscapeDataString(Input.Email)}";

            var html = $"""
                <p>{localizer["Email.Greeting"].Value}</p>
                <p>{localizer["Email.Body"].Value}</p>
                <p><a href="{resetUrl}">{localizer["Email.LinkText"].Value}</a></p>
                <p style="color:#8C8980;font-size:0.85rem;">{localizer["Email.Expiry"].Value}</p>
                <p style="color:#8C8980;font-size:0.85rem;">{localizer["Email.Ignore"].Value}</p>
                """;

            await emailService.SendAsync(Input.Email, localizer["Email.Subject"], html);
        }

        return Redirect("/account/forgot-password-confirmation");
    }
}
