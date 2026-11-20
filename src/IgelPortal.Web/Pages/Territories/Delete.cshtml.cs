using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Territories
{
    [Authorize(Roles = RoleNames.USER)]
    public class DeleteModel(ITerritoryDataService dataService, ICommunityDataService communityDataService) : PageModel
    {
        private readonly ITerritoryDataService dataService = dataService;
        private readonly ICommunityDataService communityDataService = communityDataService;

        [BindProperty]
        public Territory Territory { get; set; } = default!;

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
            else
            {
                this.Territory = territory;
            }
            return base.Page();
        }

        public async Task<IActionResult> OnPostAsync(String? filter)
        {
            Guid? id = this.Territory?.Id;
            if (!id.HasValue)
            {
                return base.NotFound();
            }

            var territory = await this.dataService.FindOne(id.Value);
            if (territory != null)
            {
                Territory = territory;
                await this.dataService.Delete(id.Value);
            }

            return base.RedirectToPage("./Index", new { filter });
        }
    }
}
