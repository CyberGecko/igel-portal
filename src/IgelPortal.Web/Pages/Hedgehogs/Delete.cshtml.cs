using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class DeleteModel(IHedgehogDataService dataService) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;

        [BindProperty]
        public Hedgehog Hedgehog { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id, String? filter)
        {
            base.ViewData["Filter"] = filter;
            if (!id.HasValue)
            {
                return base.NotFound();
            }

            Hedgehog? hedgehog = await this.dataService.FindOne(id.Value);

            if (hedgehog is null || hedgehog.IsDeleted)
            {
                return base.NotFound();
            }
            if (hedgehog.AttachedUser != User.Identity?.Name)
            {
                return Forbid();
            }
            this.Hedgehog = hedgehog;
            return base.Page();
        }

        public async Task<IActionResult> OnPostAsync(String? filter)
        {
            Guid? id = this.Hedgehog?.Id;
            if (!id.HasValue)
            {
                return base.NotFound();
            }

            var hedgehog = await dataService.FindOne(id);
            if (hedgehog is null || hedgehog.IsDeleted)
            {
                return NotFound();
            }
            if (hedgehog.AttachedUser != User.Identity?.Name)
            {
                return Forbid();
            }

            await dataService.Delete(id.Value);
            return base.RedirectToPage("./Index", new { filter });
        }
    }
}
