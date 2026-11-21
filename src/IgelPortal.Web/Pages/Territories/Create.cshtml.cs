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

        public async Task<IActionResult> OnGetAsync(string? filter)
        {
            Territory.CommunityId = await LoadChoicesAsync(filter);
            return Page();
        }

        private async Task<Guid?> LoadChoicesAsync(string? filter)
        {
            ViewData["Filter"] = filter;
            var groups = await CommunityFormChoices.LoadAsync(communityDataService, User.Identity, filter);
            ViewData["Communities"] = new SelectList(groups.Options, "Id", "Name");

            return groups.Selected;
        }

        [BindProperty]
        public Territory Territory { get; set; } = new();

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(String? filter)
        {
            ModelState.Remove("Territory.UserName");
            ModelState.Remove("Territory.Community");
            if (!ModelState.IsValid)
            {
                await LoadChoicesAsync(filter);
                return Page();
            }
            if (Territory.CommunityId.HasValue && !await communityDataService.IsCommunityMember(Territory.CommunityId, User.Identity))
            {
                return Forbid();
            }
            this.Territory.UserName = base.User.Identity?.Name ?? String.Empty;
            await this.dataService.Insert(this.Territory);

            TempData["RecordNotice"] = "Territory added.";
            return base.RedirectToPage("./Index", new { filter });
        }
    }
}
