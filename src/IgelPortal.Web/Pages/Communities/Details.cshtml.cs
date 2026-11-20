using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Communities
{
    [Authorize(Roles = $"{RoleNames.ADMIN},{RoleNames.USER}", Policy = PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)]
    public class DetailsModel(ICommunityDataService dataService) : PageModel
    {
        private readonly ICommunityDataService dataService = dataService;

        public Community Community { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (!id.HasValue)
            {
                return base.NotFound();
            }

            var community = await this.dataService.FindOne(id.Value);

            if (community == null)
            {
                return base.NotFound();
            }
            else
            {
                Boolean isCommunityModerator = await this.dataService.HasUserCommunityMemberRole(id, base.User.Identity, CommunityRoles.MODERATOR);
                Boolean isCommunityOwner = await this.dataService.HasUserCommunityMemberRole(id, base.User.Identity, CommunityRoles.OWNER);
                
                if (!base.User.IsInRole(RoleNames.ADMIN) && !isCommunityModerator && !isCommunityOwner)
                {
                    return base.RedirectToPage("/Index");
                }

                this.Community = community;
            }
            return base.Page();
        }
    }
}
