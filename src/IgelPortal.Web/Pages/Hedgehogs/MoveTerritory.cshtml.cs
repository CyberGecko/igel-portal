using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER, Policy = PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)]
    public class MoveTerritoryModel(IHedgehogDataService dataService, ITerritoryDataService territoryDataService) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;
        private readonly ITerritoryDataService territoryDataService = territoryDataService;

        public SelectList Territories { get; set; } = default!;

        public async Task OnGet(Guid? id, Guid? hedgehogId)
        {
            var territories = await this.territoryDataService.FindAllActiveForCommunity(id);
            this.Territories = new SelectList(territories.OrderBy(c => c.UserName).ThenBy(c => c.Name), "Id", "Name");

            Hedgehog? hedgehog = hedgehogId.HasValue ? await this.dataService.FindOne(hedgehogId.Value) : null;
            base.ViewData["CurrentTerritory"] = hedgehog?.CurrentTerritory;
            base.ViewData["HedgehogId"] = hedgehogId;
            base.ViewData["CommunityId"] = id;
        }

        public async Task<ContentResult> OnGetLoadTerritoryServices(Guid? territoryId)
        {
            IList<TerritoryAttachedService> usableServices = [];
            if (territoryId.HasValue)
            {
                var territory = await this.territoryDataService.FindOne(territoryId.Value);
                usableServices = territory?.AttachedServices?.Where(c => c.CurrentCapacity > 0)?.ToList() ?? [];
            }

            string jsonData = JsonConvert.SerializeObject(usableServices.Select(c => new { id = c.Id, name = c.TerritoryService }));

            return base.Content(jsonData, "application/json");
        }

        public async Task<IActionResult> OnPost(Guid? hedgehogId, Guid? newTerritoryId, Guid? usedServiceId, Guid? communityId)
        {
            if (!hedgehogId.HasValue)
            {
                return base.RedirectToPage("./Index", new { filter = communityId });
            }
            Hedgehog? currentHedgehog = await this.dataService.FindOne(hedgehogId.Value);
            if (currentHedgehog != null)
            {
                currentHedgehog.CurrentTerritoryId = usedServiceId.HasValue && newTerritoryId.HasValue ? newTerritoryId : currentHedgehog.HomeTerritoryId;
                currentHedgehog.UsedServiceId = usedServiceId;
                await this.dataService.Update(currentHedgehog);
            }

            return base.RedirectToPage("./Index", new { filter = communityId });
        }
    }
}
