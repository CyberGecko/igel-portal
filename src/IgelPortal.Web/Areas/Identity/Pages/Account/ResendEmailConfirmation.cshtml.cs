// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using IgelPortal.Web.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using IgelPortal.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.Text;

namespace IgelPortal.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public partial class ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, PortalMail emailSender, IStringLocalizer<ResendEmailConfirmationModel> localizer) : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly PortalMail _emailSender = emailSender;
        private readonly IStringLocalizer<ResendEmailConfirmationModel> localizer = localizer;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public ResendEmailConfirmationInputModel Input { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, this.localizer.GetString("Verification email sent. Please check your email."));
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = userId, code = code },
                protocol: Request.Scheme);
            await _emailSender.ConfirmEmailAsync(Input.Email, callbackUrl);

            ModelState.AddModelError(string.Empty, this.localizer.GetString("Verification email sent. Please check your email."));
            return Page();
        }
    }
}
