using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class EditModel(IHedgehogDataService dataService, ITerritoryDataService territoryDataService, ICommunityDataService communityDataService) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;
        private readonly ITerritoryDataService territoryDataService = territoryDataService;
        private readonly ICommunityDataService communityDataService = communityDataService;

        [BindProperty]
        public Hedgehog Hedgehog { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id, String? filter)
        {
            base.ViewData["Filter"] = filter;
            if (!id.HasValue)
            {
                return base.NotFound();
            }

            var hedgehog = await this.dataService.FindOne(id.Value);
            if (hedgehog is null || hedgehog.IsDeleted)
            {
                return base.NotFound();
            }
            if (hedgehog.AttachedUser != User.Identity?.Name)
            {
                return Forbid();
            }
            this.Hedgehog = hedgehog;

            IList<Territory> territories = await this.territoryDataService.GetAllAdministratedByUser(base.User.Identity);
            if (hedgehog.HomeTerritoryId is Guid territoryId && !territories.Any(territory => territory.Id == territoryId))
            {
                territories.Add(new Territory { Id = territoryId, Name = hedgehog.HomeTerritory });
            }
            ViewData["Territories"] = new SelectList(territories.OrderBy(c => c.Community).ThenBy(c => c.Name), "Id", "Name", null, "Community");

            var communities = await communityDataService.FindAllForUserWithRole(User.Identity, CommunityRoles.MEMBER);
            if (hedgehog.CommunityId is Guid communityId && !communities.Any(community => community.Id == communityId))
            {
                communities.Add(new Community { Id = communityId, Name = hedgehog.Community });
            }
            ViewData["Communities"] = new SelectList(communities, "Id", "Name");

            return base.Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(String? filter)
        {
            if (Hedgehog is null)
            {
                return BadRequest();
            }
            var existing = await dataService.FindOne(Hedgehog.Id);
            if (existing is null || existing.IsDeleted)
            {
                return NotFound();
            }
            if (existing.AttachedUser != User.Identity?.Name)
            {
                return Forbid();
            }
            Hedgehog.AttachedUser = existing.AttachedUser;
            Hedgehog.CurrentTerritoryId = existing.CurrentTerritoryId;
            Hedgehog.UsedServiceId = existing.UsedServiceId;
            if (Hedgehog.CommunityId != existing.CommunityId && Hedgehog.CommunityId.HasValue &&
                !await communityDataService.IsCommunityMember(Hedgehog.CommunityId, User.Identity))
            {
                return Forbid();
            }
            if (Hedgehog.HomeTerritoryId != existing.HomeTerritoryId && Hedgehog.HomeTerritoryId.HasValue &&
                !(await territoryDataService.GetAllAdministratedByUser(User.Identity)).Any(item => item.Id == Hedgehog.HomeTerritoryId))
            {
                return Forbid();
            }
            ModelState.Remove("Hedgehog.AttachedUser");
            ModelState.Remove("Hedgehog.AttachedUserDisplayName");
            base.ModelState.Remove("Hedgehog.HomeTerritory");
            base.ModelState.Remove("Hedgehog.CurrentTerritory");
            base.ModelState.Remove("Hedgehog.Community");
            base.ModelState.Remove("Hedgehog.UsedService");
            if (!ModelState.IsValid)
            {
                return await OnGetAsync(existing.Id, filter);
            }

            await this.dataService.Update(this.Hedgehog);

            TempData["RecordNotice"] = "Hedgehog updated.";
            return base.RedirectToPage("./Index", new { filter });
        }
    }
}
