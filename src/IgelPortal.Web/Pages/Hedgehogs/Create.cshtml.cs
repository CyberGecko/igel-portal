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

        public async Task<IActionResult> OnGet(String? filter)
        {
            base.ViewData["Filter"] = filter;
            IList<Territory> territories = await this.territoryDataService.GetAllAdministratedByUser(base.User.Identity);
            base.ViewData["Territories"] = new SelectList(territories.OrderBy(c => c.Community).ThenBy(c => c.Name), "Id", "Name", null, "Community");

            base.ViewData["Communities"] = new SelectList(await this.communityDataService.FindAllForUserWithRole(base.User.Identity, CommunityRoles.MEMBER), "Id", "Name");
            return base.Page();
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
                return await OnGet(filter);
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
