using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Areas.Identity.Pages.Account;

public class PasskeyLoginModel : PageModel
{
    public IActionResult OnGet(string? returnUrl) =>
        RedirectToPage("./Login", new { returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
}
