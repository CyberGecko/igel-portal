// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Areas.Identity.Pages.Account.Manage
{
    public partial class PasskeyAuthenticationModel(IFido2DataService fido2DataService) : PageModel
    {
        public string ReturnUrl { get; set; }
        public IList<PublicKeyCredential> UserFidoCredentials { get; set; } = [];
        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/Identity/Account/Manage/PasskeyAuthentication");
            UserFidoCredentials = await fido2DataService.GetUserFidoCredentials(User.Identity);
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/Identity/Account/Manage/PasskeyAuthentication");
            await fido2DataService.RemoveAuthentication(User.Identity);

            return RedirectToPage(new { ReturnUrl });
        }
    }
}
