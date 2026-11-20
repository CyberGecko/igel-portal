using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class IndexModel(IHedgehogDataService dataService, ICommunityDataService communityDataService) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;
        private readonly ICommunityDataService communityDataService = communityDataService;

        public IList<Hedgehog> Hedgehogs { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(String? filter)
        {
            base.ViewData["Filter"] = filter;
            if (filter is null or "my")
            {
                base.ViewData["IsCommunityView"] = false;
                this.Hedgehogs = await this.dataService.FindAllActiveForUser(User?.Identity?.Name);
            }
            else if (Guid.TryParse(filter, out Guid communityId))
            {
                Boolean isCommunityModerator = await this.communityDataService.HasUserCommunityMemberRole(communityId, base.User.Identity, CommunityRoles.MODERATOR);
                Boolean isCommunityOwner = await this.communityDataService.HasUserCommunityMemberRole(communityId, base.User.Identity, CommunityRoles.OWNER);
                if (!base.User.IsInRole(RoleNames.ADMIN) && !isCommunityModerator && !isCommunityOwner)
                {
                    return base.RedirectToPage("/Index");
                }

                var communityHedgehogs = await this.dataService.FindAllActiveForCommunity(communityId);
                this.Hedgehogs = communityHedgehogs.OrderBy(c => c.AttachedUser).ThenBy(c => c.Name).ToList();
                base.ViewData["IsCommunityView"] = true;
            }
            else
            {
                return new RedirectToPageResult("/Index");
            }

            return base.Page();
        }
        
    }
}
