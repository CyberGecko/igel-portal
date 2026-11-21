using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class MoveTerritoryModel(IHedgehogDataService dataService, ITerritoryDataService territoryDataService, IAuthorizationService authorization) : PageModel
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

            base.ViewData["CurrentTerritory"] = hedgehog?.CurrentTerritory;
            base.ViewData["HedgehogId"] = hedgehogId;
            base.ViewData["CommunityId"] = id;
            return Page();
        }

        public async Task<IActionResult> OnGetLoadTerritoryServices(Guid? id, Guid? territoryId)
        {
            if (!await CanManage(id))
            {
                return Forbid();
            }
            var territory = await territoryDataService.FindOne(territoryId);
            if (territory is null || territory.IsDeleted || territory.CommunityId != id)
            {
                return NotFound();
            }
            return new JsonResult(territory.AttachedServices.Where(service => !service.IsPaused && service.CurrentCapacity > 0)
                .Select(service => new { id = service.Id, name = service.TerritoryService }));
        }

        public async Task<IActionResult> OnPost(Guid? hedgehogId, Guid? newTerritoryId, Guid? usedServiceId, Guid? communityId)
        {
            if (!await CanManage(communityId))
            {
                return Forbid();
            }
            if (!hedgehogId.HasValue)
            {
                return base.RedirectToPage("./Index", new { filter = communityId });
            }
            Hedgehog? currentHedgehog = await this.dataService.FindOne(hedgehogId.Value);
            if (currentHedgehog is null || currentHedgehog.IsDeleted || currentHedgehog.CommunityId != communityId)
            {
                return NotFound();
            }

            if (usedServiceId.HasValue)
            {
                var territory = await territoryDataService.FindOne(newTerritoryId);
                if (territory is null || territory.IsDeleted || territory.CommunityId != communityId ||
                    !territory.AttachedServices.Any(service => service.Id == usedServiceId && !service.IsPaused && service.CurrentCapacity > 0))
                {
                    return BadRequest();
                }
            }

            currentHedgehog.CurrentTerritoryId = usedServiceId.HasValue ? newTerritoryId : currentHedgehog.HomeTerritoryId;
            currentHedgehog.UsedServiceId = usedServiceId;
            await dataService.Update(currentHedgehog);

            return base.RedirectToPage("./Index", new { filter = communityId });
        }
        private async Task<bool> CanManage(Guid? communityId) =>
            communityId.HasValue && (await authorization.AuthorizeAsync(User, communityId.Value, PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)).Succeeded;
    }
}
