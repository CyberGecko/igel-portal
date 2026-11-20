using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace IgelPortal.Web.Pages;

public class IndexModel(IOptions<RequestLocalizationOptions> localization) : PageModel
{
    public IActionResult OnPostSetLanguage(string? culture, string? returnUrl)
    {
        var supported = localization.Value.SupportedUICultures?
            .FirstOrDefault(item => string.Equals(item.Name, culture, StringComparison.OrdinalIgnoreCase));
        if (supported is not null)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(supported)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true
                });
        }

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToPage("/Index");
    }
}
