using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;

namespace IgelPortal.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
[EnableRateLimiting(PasskeyFlow.RateLimitPolicy)]
[RequestSizeLimit(65_536)]
public class PasskeyAuthenticationModel(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
    PasskeyFlow flow, IStringLocalizer<PasskeyAuthenticationModel> localizer) : PageModel
{
    public const int NameLength = 60;
    public bool RecentlyAuthenticated { get; private set; }
    public IReadOnlyList<UserPasskeyInfo> Passkeys { get; private set; } = [];

    [TempData]
    public string? Status { get; set; }

    [TempData]
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        RecentlyAuthenticated = await PortalSignInManager.HasRecentAuthenticationAsync(HttpContext);
        Passkeys = (await users.GetPasskeysAsync(user)).OrderByDescending(key => key.CreatedAt).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateOptionsAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        Response.Headers.CacheControl = "no-store";
        return Content(await signIn.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = user.Id,
            Name = user.UserName!,
            DisplayName = user.ToString()
        }), "application/json");
    }

    public async Task<IActionResult> OnPostCreateAsync(string? name, string? credential)
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var keyName = name?.Trim();
        if (keyName is { Length: > 0 and <= NameLength } &&
            credential is { Length: > 0 and <= PasskeyFlow.CredentialSizeLimit } &&
            await flow.TryConsumeAsync(HttpContext, registration: true))
        {
            var result = await signIn.PerformPasskeyAttestationAsync(credential);
            if (result.Succeeded && result.UserEntity.Id == user.Id)
            {
                result.Passkey.Name = keyName;
                if ((await users.AddOrUpdatePasskeyAsync(user, result.Passkey)).Succeeded)
                {
                    Status = localizer["Passkey added."];
                    return RedirectToPage();
                }
            }
        }

        Error = localizer["The passkey could not be added. Please try again."];
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string? keyId)
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (keyId is { Length: > 0 and <= 1364 })
        {
            try
            {
                var credentialId = WebEncoders.Base64UrlDecode(keyId);
                if ((await users.RemovePasskeyAsync(user, credentialId)).Succeeded)
                {
                    Status = localizer["Passkey removed."];
                    return RedirectToPage();
                }
            }
            catch (FormatException)
            {
            }
        }

        Error = localizer["The passkey could not be removed."];
        return RedirectToPage();
    }
}
