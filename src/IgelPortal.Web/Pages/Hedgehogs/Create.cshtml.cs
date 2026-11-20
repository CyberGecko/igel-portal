using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class CreateModel(IHedgehogDataService dataService, ITerritoryDataService territoryDataService, ICommunityDataService communityDataService) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;
        private readonly ITerritoryDataService territoryDataService = territoryDataService;
        private readonly ICommunityDataService communityDataService = communityDataService;

        public async Task<IActionResult> OnGet(String? filter)
        {
            base.ViewData["Filter"] = filter;
            IList<Territory> territories = await this.territoryDataService.GetAllAdministratedByUser(base.User.Identity);
            base.ViewData["Territories"] = new SelectList(territories.OrderBy(c => c.Community).ThenBy(c => c.Name), "Id", "Name", null, "Community");

            base.ViewData["Communities"] = new SelectList(await this.communityDataService.FindAllForUserWithRole(base.User.Identity, CommunityRoles.MEMBER), "Id", "Name");
            return base.Page();
        }

        [BindProperty]
        public Hedgehog Hedgehog { get; set; } = default!;


        public async Task<IActionResult> OnPostAsync(String? filter)
        {
            base.ModelState.Remove("Hedgehog.HomeTerritory");
            base.ModelState.Remove("Hedgehog.CurrentTerritory");
            base.ModelState.Remove("Hedgehog.Community");
            base.ModelState.Remove("Hedgehog.UsedService");
            if (!base.ModelState.IsValid)
            {
                return base.Page();
            }

            if (String.IsNullOrWhiteSpace(base.User?.Identity?.Name))
            {
                return base.RedirectToPage("./Index", new { filter });
            }

            this.Hedgehog.AttachedUser = base.User.Identity.Name;
            this.Hedgehog.CurrentTerritoryId = this.Hedgehog.HomeTerritoryId;

            if (!String.IsNullOrEmpty(this.Hedgehog.Image))
            {
                String imageString = this.Hedgehog.Image.Replace("data:image/png;base64,", "");
                using MagickImage image = new(Convert.FromBase64String(imageString));
                if (image.Width > 192 || image.Height > 192)
                {
                    MagickGeometry size = new(192, 192);
                    image.Resize(size);
                    using MemoryStream ms = new();
                    image.Write(ms);
                    this.Hedgehog.Image = $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
                }
            }

            await this.dataService.Insert(this.Hedgehog);

            return base.RedirectToPage("./Index", new { filter });
        }
    }
}
