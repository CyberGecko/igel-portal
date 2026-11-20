using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Administration
{
    [Authorize(Roles = RoleNames.ADMIN)]
    public class TerritoryServicesModel(ITerritoryServiceDataService dataService) : PageModel
    {
        private readonly ITerritoryServiceDataService dataService = dataService;

        public IList<TerritoryService> TerritoryServices { get; set; } = default!;
        [BindProperty]
        public TerritoryService TerritoryService { get; set; } = default!;

        public async Task OnGetAsync()
        {
            this.TerritoryServices = await this.dataService.FindAll();
        }

        public async Task<IActionResult> OnGetEdit(Guid? id)
        {
            if (!id.HasValue)
            {
                return base.NotFound();
            }

            TerritoryService? territoryService = await this.dataService.FindOne(id.Value);

            // concurrency check if someone deleted the record 
            if (territoryService != null)
            {

                // set the bind property 
                this.TerritoryService = territoryService;

            }

            // fill the products collection 
            this.TerritoryServices = await this.dataService.FindAll();

            // Model and bind properties are filled 
            // render the page now 
            return base.Page();

        }

        public async Task<IActionResult> OnPost()
        {
            if (this.TerritoryService.Id == default)
            {
                await this.dataService.Insert(this.TerritoryService);
            }
            else
            {
                await this.dataService.Update(this.TerritoryService);
            }

            return base.RedirectToPage();

        }

        public async Task<IActionResult> OnPostDelete(Guid? id)
        {
            if (!id.HasValue)
            {
                return base.NotFound();
            }

            TerritoryService? territoryService = await this.dataService.FindOne(id.Value);
            if (territoryService != null)
            {
                this.TerritoryService = territoryService;
                await this.dataService.Delete(id.Value);
            }

            return base.RedirectToPage();

        }

    }
}
