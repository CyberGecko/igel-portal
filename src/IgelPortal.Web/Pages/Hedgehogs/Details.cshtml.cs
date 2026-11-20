using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class DetailsModel(IHedgehogDataService dataService, IAuthorizationService authorization) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;

        public Hedgehog Hedgehog { get; set; } = default!;

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
            if (hedgehog.AttachedUser != User.Identity?.Name && !User.IsInRole(RoleNames.ADMIN) &&
                (!hedgehog.CommunityId.HasValue || !(await authorization.AuthorizeAsync(User, hedgehog.CommunityId.Value, PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)).Succeeded))
            {
                return Forbid();
            }
            this.Hedgehog = hedgehog;
            return base.Page();
        }
    }
}
