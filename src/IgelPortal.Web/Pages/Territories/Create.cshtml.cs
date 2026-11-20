using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Territories
{
    [Authorize(Roles = RoleNames.USER)]
    public class CreateModel(ITerritoryDataService dataService, ICommunityDataService communityDataService) : PageModel
    {
        private readonly ITerritoryDataService dataService = dataService;
        private readonly ICommunityDataService communityDataService = communityDataService;

        public async Task<IActionResult> OnGetAsync(String? filter)
        {
            base.ViewData["Filter"] = filter;
            IEnumerable<Community> communities = await this.communityDataService.FindAllForUserWithRole(base.User.Identity, CommunityRoles.MEMBER);
            base.ViewData["Communities"] = new SelectList(communities, "Id", "Name");

            return base.Page();
        }

        [BindProperty]
        public Territory Territory { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(String? filter)
        {
            ModelState.Remove("Territory.Community");
            if (!ModelState.IsValid)
            {
                return base.Page();
            }
            this.Territory.UserName = base.User.Identity?.Name ?? String.Empty;
            await this.dataService.Insert(this.Territory);

            return base.RedirectToPage("./Index", new { filter });
        }
    }
}
