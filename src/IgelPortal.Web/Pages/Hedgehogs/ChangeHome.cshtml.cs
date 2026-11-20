using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER, Policy = PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)]
    public class ChangeHomeModel(IHedgehogDataService dataService, ITerritoryDataService territoryDataService) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;
        private readonly ITerritoryDataService territoryDataService = territoryDataService;

        public SelectList Territories { get; set; } = default!;

        public async Task OnGet(Guid? id, Guid? hedgehogId)
        {
            var territories = await this.territoryDataService.FindAllActiveForCommunity(id);
            this.Territories = new SelectList(territories.OrderBy(c => c.UserName).ThenBy(c => c.Name), "Id", "Name");

            Hedgehog? hedgehog = hedgehogId.HasValue ? await this.dataService.FindOne(hedgehogId.Value) : null;

            base.ViewData["CurrentHomeTerritory"] = hedgehog?.HomeTerritory;
            base.ViewData["HedgehogId"] = hedgehogId;
            base.ViewData["CommunityId"] = id;
        }

        public async Task<IActionResult> OnPost(Guid? hedgehogId, Guid? newHomeTerritoryId, Guid? communityId)
        {
            if (!hedgehogId.HasValue || !newHomeTerritoryId.HasValue)
            {
                return base.RedirectToPage();
            }
            Hedgehog? currentHedgehog = await this.dataService.FindOne(hedgehogId.Value);
            Territory? newHomeTerritory = await this.territoryDataService.FindOne(newHomeTerritoryId.Value);
            if (currentHedgehog != null && newHomeTerritory != null)
            {
                currentHedgehog.HomeTerritoryId = newHomeTerritoryId;
                await this.dataService.Update(currentHedgehog);
            }

            return base.RedirectToPage("./Index", new { filter = communityId });
        }
    }
}
