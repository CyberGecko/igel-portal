using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Territories
{
    [Authorize(Roles = RoleNames.USER)]
    public class EditModel(ITerritoryDataService dataService, ICommunityDataService communityDataService, ITerritoryServiceDataService territoryServiceDataService, ITerritoryAttachedServiceDataService territoryAttachedServiceDataService) : PageModel
    {
        private readonly ITerritoryDataService dataService = dataService;
        private readonly ICommunityDataService communityDataService = communityDataService;
        private readonly ITerritoryServiceDataService territoryServiceDataService = territoryServiceDataService;
        private readonly ITerritoryAttachedServiceDataService territoryAttachedServiceDataService = territoryAttachedServiceDataService;

        [BindProperty]
        public Territory Territory { get; set; } = default!;
        [BindProperty]
        public TerritoryAttachedService TerritoryAttachedService { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id, String? filter)
        {
            base.ViewData["Filter"] = filter;
            if (!id.HasValue)
            {
                return base.NotFound();
            }

            var territory = await this.dataService.FindOne(id.Value);
            if (territory == null)
            {
                return base.NotFound();
            }
            this.Territory = territory;

            IEnumerable<Community> communities = await this.communityDataService.FindAllForUserWithRole(base.User.Identity, CommunityRoles.MEMBER);
            base.ViewData["Communities"] = new SelectList(communities, "Id", "Name", this.Territory.CommunityId);

            IEnumerable<TerritoryService> territoryServices = await this.territoryServiceDataService.FindAll();
            base.ViewData["TerritoryServices"] = new SelectList(territoryServices, "Id", "Name");

            return base.Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(String? filter)
        {
            ModelState.Remove("Territory");
            ModelState.Remove("TerritoryService");
            ModelState.Remove("Territory.Community");
            if (!base.ModelState.IsValid)
            {
                return base.Page();
            }
            await this.dataService.Update(this.Territory);

            return base.RedirectToPage("./Index", new { filter });
        }

        public async Task<IActionResult> OnPostAddRow(Guid territoryId, String? filter)
        {
            Territory? territory = await this.dataService.FindOne(territoryId);
            if (territory != null)
            {
                this.TerritoryAttachedService.TerritoryId = territoryId;
                if (this.TerritoryAttachedService.Id == default)
                {
                    await this.territoryAttachedServiceDataService.Insert(this.TerritoryAttachedService);
                }
                else
                {
                    await this.territoryAttachedServiceDataService.Update(this.TerritoryAttachedService);
                }
            }
            return base.RedirectToPage(new { id = territoryId, filter });
        }
        public async Task<IActionResult> OnPostRemoveRow(Guid territoryId, Guid id, String? filter)
        {
            Territory? territory = await this.dataService.FindOne(territoryId);
            if (territory != null)
            {
                await this.territoryAttachedServiceDataService.Delete(id);
            }
            return base.RedirectToPage(new { id = territoryId ,filter});
        }
    }
}
