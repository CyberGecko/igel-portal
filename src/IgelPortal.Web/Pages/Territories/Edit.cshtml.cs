using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Territories
{
    [Authorize(Roles = RoleNames.USER)]
    public class EditModel(ITerritoryDataService dataService, ICommunityDataService communityDataService, ITerritoryServiceDataService territoryServiceDataService, ITerritoryAttachedServiceDataService territoryAttachedServiceDataService, IAuthorizationService authorization) : PageModel
    {
        private readonly ITerritoryDataService dataService = dataService;
        private readonly ICommunityDataService communityDataService = communityDataService;
        private readonly ITerritoryServiceDataService territoryServiceDataService = territoryServiceDataService;
        private readonly ITerritoryAttachedServiceDataService territoryAttachedServiceDataService = territoryAttachedServiceDataService;

        [BindProperty]
        public Territory Territory { get; set; } = default!;
        [BindProperty]
        public TerritoryAttachedService TerritoryAttachedService { get; set; } = new();

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
            if (!await CanEdit(territory))
            {
                return Forbid();
            }
            Territory = territory;
            ViewData["KeepTerritoryCommunity"] = territory.UserName != User.Identity?.Name;

            var communities = await communityDataService.FindAllForUserWithRole(User.Identity, CommunityRoles.MEMBER);
            if (territory.CommunityId is Guid communityId && !communities.Any(community => community.Id == communityId))
            {
                communities.Add(new Community { Id = communityId, Name = territory.Community });
            }
            ViewData["Communities"] = new SelectList(communities, "Id", "Name", territory.CommunityId);

            IEnumerable<TerritoryService> territoryServices = await this.territoryServiceDataService.FindAll();
            base.ViewData["TerritoryServices"] = new SelectList(territoryServices, "Id", "Name");

            return base.Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(String? filter)
        {
            if (Territory is null)
            {
                return BadRequest();
            }
            var existing = await dataService.FindOne(Territory.Id);
            if (existing is null || existing.IsDeleted)
            {
                return NotFound();
            }
            if (existing.UserName != User.Identity?.Name && !ModelState.ContainsKey("Territory.CommunityId"))
            {
                Territory.CommunityId = existing.CommunityId;
            }
            if (!await CanEdit(existing) ||
                (Territory.CommunityId != existing.CommunityId &&
                 (existing.UserName != User.Identity?.Name ||
                  (Territory.CommunityId.HasValue && !await communityDataService.IsCommunityMember(Territory.CommunityId, User.Identity)))))
            {
                return Forbid();
            }
            Territory.UserName = existing.UserName;
            ModelState.Remove("Territory.UserName");
            ModelState.Remove("Territory");
            ModelState.Remove("TerritoryService");
            ModelState.Remove("Territory.Community");
            if (!base.ModelState.IsValid)
            {
                return await OnGetAsync(existing.Id, filter);
            }
            await this.dataService.Update(this.Territory);

            TempData["RecordNotice"] = "Territory updated.";
            return base.RedirectToPage("./Index", new { filter });
        }

        public async Task<IActionResult> OnPostAddRow(Guid territoryId, String? filter)
        {
            var territory = await dataService.FindOne(territoryId);
            if (territory is null || territory.IsDeleted)
            {
                return NotFound();
            }
            if (!await CanEdit(territory))
            {
                return Forbid();
            }

            var attached = TerritoryAttachedService;
            if (attached is null || attached.MaxCapacity < 0)
            {
                return BadRequest();
            }

            var service = await territoryServiceDataService.FindOne(attached.TerritoryServiceId);
            if (service is null || service.IsDeleted)
            {
                return BadRequest();
            }

            attached.TerritoryId = territoryId;
            if (attached.Id == default)
            {
                await territoryAttachedServiceDataService.Insert(attached);
            }
            else if (territory.AttachedServices.Any(item => item.Id == attached.Id))
            {
                await territoryAttachedServiceDataService.Update(attached);
            }
            else
            {
                return NotFound();
            }

            return RedirectToPage(new { id = territoryId, filter });
        }

        public async Task<IActionResult> OnPostRemoveRow(Guid territoryId, Guid id, string? filter)
        {
            var territory = await dataService.FindOne(territoryId);
            if (territory is null || territory.IsDeleted || !territory.AttachedServices.Any(item => item.Id == id))
            {
                return NotFound();
            }
            if (!await CanEdit(territory))
            {
                return Forbid();
            }

            await territoryAttachedServiceDataService.Delete(id);
            return RedirectToPage(new { id = territoryId, filter });
        }

        private async Task<bool> CanEdit(Territory territory) =>
            territory.UserName == User.Identity?.Name ||
            (territory.CommunityId.HasValue &&
             (await authorization.AuthorizeAsync(User, territory.CommunityId.Value, PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)).Succeeded);
    }
}
