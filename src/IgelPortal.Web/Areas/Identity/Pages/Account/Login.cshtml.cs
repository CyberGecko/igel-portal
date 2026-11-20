// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace IgelPortal.Web.Areas.Identity.Pages.Account
{
    [EnableRateLimiting(PasskeyFlow.RateLimitPolicy)]
    [RequestSizeLimit(65_536)]
    public partial class LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger,
        PasskeyFlow passkeyFlow, IStringLocalizer<LoginModel> localizer) : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly ILogger<LoginModel> _logger = logger;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public LoginInputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        public bool ConfirmAccount { get; private set; }

        [TempData]
        public string PasskeyError { get; set; }

        public async Task OnGetAsync(string returnUrl = null, bool confirmAccount = false)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            ReturnUrl = LocalReturnUrl(returnUrl);
            ConfirmAccount = confirmAccount;

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null, bool confirmAccount = false)
        {
            ReturnUrl = LocalReturnUrl(returnUrl);
            ConfirmAccount = confirmAccount;

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(ReturnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        public async Task<IActionResult> OnPostPasskeyOptionsAsync()
        {
            Response.Headers.CacheControl = "no-store";
            return Content(await _signInManager.MakePasskeyRequestOptionsAsync(user: null), "application/json");
        }

        public async Task<IActionResult> OnPostPasskeyAsync(string credential, string returnUrl = null, bool confirmAccount = false)
        {
            if (credential is { Length: > 0 and <= PasskeyFlow.CredentialSizeLimit } &&
                await passkeyFlow.TryConsumeAsync(HttpContext))
            {
                var result = await _signInManager.PasskeySignInAsync(credential);
                if (result.Succeeded)
                {
                    return LocalRedirect(LocalReturnUrl(returnUrl));
                }
            }

            PasskeyError = localizer["Passkey sign-in failed. Try again or use another sign-in method."];
            return RedirectToPage(new { returnUrl = LocalReturnUrl(returnUrl), confirmAccount });
        }

        private string LocalReturnUrl(string url) => Url.IsLocalUrl(url) ? url : Url.Content("~/");
    }
}
