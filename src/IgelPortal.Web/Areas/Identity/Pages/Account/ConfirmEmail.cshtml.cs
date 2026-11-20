// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using IgelPortal.Web.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.Text;

namespace IgelPortal.Web.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IStringLocalizer<RegisterModel> localizer) : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager = userManager;
        private readonly RoleManager<IdentityRole> roleManager = roleManager;
        private readonly IStringLocalizer<RegisterModel> localizer = localizer;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await this.userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(this.localizer.GetString("Unable to load user with ID '{0}'.", userId));
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await this.userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                StatusMessage = this.localizer.GetString("Thank you for confirming your email.");
                await this.userManager.AddToRoleAsync(user, RoleNames.USER);
            }
            else
            {
                StatusMessage = this.localizer.GetString("Error confirming your email.");
            }
            return Page();
        }
    }
}
