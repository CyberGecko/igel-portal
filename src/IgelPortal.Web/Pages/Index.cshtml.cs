using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages
{
    public class IndexModel : PageModel
    {

        public void OnGet()
        {
        }

        public IActionResult OnPostSetLanguage(String culture, String returnUrl)
        {

            RequestCulture requestCulture = new(culture);
            base.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                                         CookieRequestCultureProvider.MakeCookieValue(requestCulture),
                                         new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

            return base.LocalRedirect(returnUrl);
        }
    }
}
