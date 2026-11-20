using System.Globalization;

namespace IgelPortal.Web.Models
{
    public class CultureSwitcherModel
    {
        public CultureInfo CurrentUICulture { get; set; } = default!;
        public List<CultureInfo> SupportedCultures { get; set; } = [];
        public String ReturnUrl { get; set; } = default!;
    }
}
