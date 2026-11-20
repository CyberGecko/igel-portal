using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Territories
{
    [Authorize(Roles = RoleNames.USER)]
    public class DetailsModel(ITerritoryDataService dataService, ICommunityDataService communityDataService) : PageModel
    {
        private readonly ITerritoryDataService dataService = dataService;
        private readonly ICommunityDataService communityDataService = communityDataService;

        public Territory Territory { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id, String? filter)
        {
            base.ViewData["Filter"] = filter;
            if (!id.HasValue)
            {
                return base.NotFound();
            }

            var territory = await this.dataService.FindOne(id.Value);
            if (territory is null || territory.IsDeleted)
            {
                return base.NotFound();
            }
            if (territory.UserName != User.Identity?.Name && !User.IsInRole(RoleNames.ADMIN) &&
                !await communityDataService.IsCommunityMember(territory.CommunityId, User.Identity))
            {
                return Forbid();
            }
            this.Territory = territory;
            return base.Page();
        }
    }
}
