using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class CreateModel(IHedgehogDataService dataService, ITerritoryDataService territoryDataService, ICommunityDataService communityDataService) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;
        private readonly ITerritoryDataService territoryDataService = territoryDataService;
        private readonly ICommunityDataService communityDataService = communityDataService;

        public async Task<IActionResult> OnGetAsync(string? filter)
        {
            Hedgehog.CommunityId = await LoadChoicesAsync(filter);
            return Page();
        }

        private async Task<Guid?> LoadChoicesAsync(string? filter)
        {
            ViewData["Filter"] = filter;
            var groups = await CommunityFormChoices.LoadAsync(communityDataService, User.Identity, filter);
            ViewData["Communities"] = new SelectList(groups.Options, "Id", "Name");
            var territories = await territoryDataService.GetAllAdministratedByUser(User.Identity);
            ViewData["Territories"] = new SelectList(territories.OrderBy(item => item.Community).ThenBy(item => item.Name),
                "Id", "Name", null, "Community");

            return groups.Selected;
        }

        [BindProperty]
        public Hedgehog Hedgehog { get; set; } = new();


        public async Task<IActionResult> OnPostAsync(String? filter)
        {
            ModelState.Remove("Hedgehog.AttachedUser");
            ModelState.Remove("Hedgehog.AttachedUserDisplayName");
            base.ModelState.Remove("Hedgehog.HomeTerritory");
            base.ModelState.Remove("Hedgehog.CurrentTerritory");
            base.ModelState.Remove("Hedgehog.Community");
            base.ModelState.Remove("Hedgehog.UsedService");
            if (!ModelState.IsValid)
            {
                await LoadChoicesAsync(filter);
                return Page();
            }

            if (String.IsNullOrWhiteSpace(base.User?.Identity?.Name))
            {
                return base.RedirectToPage("./Index", new { filter });
            }

            if (Hedgehog.CommunityId.HasValue && !await communityDataService.IsCommunityMember(Hedgehog.CommunityId, User.Identity))
            {
                return Forbid();
            }
            if (Hedgehog.HomeTerritoryId.HasValue &&
                !(await territoryDataService.GetAllAdministratedByUser(User.Identity)).Any(item => item.Id == Hedgehog.HomeTerritoryId))
            {
                return Forbid();
            }

            Hedgehog.UsedServiceId = null;
            this.Hedgehog.AttachedUser = base.User.Identity.Name;
            this.Hedgehog.CurrentTerritoryId = this.Hedgehog.HomeTerritoryId;

            await this.dataService.Insert(this.Hedgehog);

            TempData["RecordNotice"] = "Hedgehog added.";
            return base.RedirectToPage("./Index", new { filter });
        }
    }
}
