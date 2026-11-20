using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class ChangeHomeModel(IHedgehogDataService dataService, ITerritoryDataService territoryDataService, IAuthorizationService authorization) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;
        private readonly ITerritoryDataService territoryDataService = territoryDataService;

        public SelectList Territories { get; set; } = default!;

        public async Task<IActionResult> OnGet(Guid? id, Guid? hedgehogId)
        {
            if (!await CanManage(id))
            {
                return Forbid();
            }

            var hedgehog = await dataService.FindOne(hedgehogId);
            if (hedgehog is null || hedgehog.IsDeleted || hedgehog.CommunityId != id)
            {
                return NotFound();
            }

            var territories = await this.territoryDataService.FindAllActiveForCommunity(id);
            this.Territories = new SelectList(territories.OrderBy(c => c.UserName).ThenBy(c => c.Name), "Id", "Name");


            base.ViewData["CurrentHomeTerritory"] = hedgehog?.HomeTerritory;
            base.ViewData["HedgehogId"] = hedgehogId;
            base.ViewData["CommunityId"] = id;
            return Page();
        }

        public async Task<IActionResult> OnPost(Guid? hedgehogId, Guid? newHomeTerritoryId, Guid? communityId)
        {
            if (!await CanManage(communityId))
            {
                return Forbid();
            }
            if (!hedgehogId.HasValue || !newHomeTerritoryId.HasValue)
            {
                return base.RedirectToPage();
            }
            Hedgehog? currentHedgehog = await this.dataService.FindOne(hedgehogId.Value);
            Territory? newHomeTerritory = await this.territoryDataService.FindOne(newHomeTerritoryId.Value);
            if (currentHedgehog is null || currentHedgehog.IsDeleted || currentHedgehog.CommunityId != communityId ||
                newHomeTerritory is null || newHomeTerritory.IsDeleted || newHomeTerritory.CommunityId != communityId)
            {
                return NotFound();
            }
            currentHedgehog.HomeTerritoryId = newHomeTerritoryId;
            await dataService.Update(currentHedgehog);

            return base.RedirectToPage("./Index", new { filter = communityId });
        }
        private async Task<bool> CanManage(Guid? communityId) =>
            communityId.HasValue && (await authorization.AuthorizeAsync(User, communityId.Value, PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)).Succeeded;
    }
}
