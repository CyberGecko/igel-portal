using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Communities
{
    [Authorize(Roles = RoleNames.ADMIN)]
    public class IndexModel(ICommunityDataService dataService) : PageModel
    {
        private readonly ICommunityDataService dataService = dataService;

        public IList<Community> Communities { get; set; } = default!;

        public async Task OnGetAsync()
        {
            this.Communities = await this.dataService.FindAll();
        }
    }
}
