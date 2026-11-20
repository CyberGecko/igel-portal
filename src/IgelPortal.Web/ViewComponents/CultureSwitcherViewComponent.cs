using IgelPortal.Web.Models;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IgelPortal.Web.ViewComponents
{
    public class CultureSwitcherViewComponent(IOptions<RequestLocalizationOptions> localizationOptions) : ViewComponent
    {
        private readonly IOptions<RequestLocalizationOptions> localizationOptions = localizationOptions;

        public IViewComponentResult Invoke(Boolean showCurrentCulture = false)
        {
            base.ViewData["ShowCurrentCulture"] = showCurrentCulture;
            var cultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var model = new CultureSwitcherModel
            {
                SupportedCultures = [.. localizationOptions.Value.SupportedUICultures ?? []],
                CurrentUICulture = cultureFeature?.RequestCulture?.UICulture ?? new System.Globalization.CultureInfo("de-DE"),
                ReturnUrl = string.IsNullOrEmpty(base.HttpContext.Request.Path) ? "~/" : $"~{base.HttpContext.Request.Path.Value}"
            };
            return View(model);
        }
    }
}
