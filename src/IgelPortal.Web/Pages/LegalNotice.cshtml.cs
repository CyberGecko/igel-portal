using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages
{
    public class LegalNoticeModel : PageModel
    {
        private readonly ILogger<LegalNoticeModel> _logger;

        public LegalNoticeModel(ILogger<LegalNoticeModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }

}
