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

        public bool CanManageCommunity { get; private set; }
        public HashSet<Guid> ManagedGroups { get; private set; } = [];

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
                CanManageCommunity = isCommunityModerator || isCommunityOwner;
                if (!User.IsInRole(RoleNames.ADMIN) && !CanManageCommunity)
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

            if (filter is null or "my")
            {
                var owned = await communityDataService.FindAllForUserWithRole(User?.Identity, CommunityRoles.OWNER);
                var moderated = await communityDataService.FindAllForUserWithRole(User?.Identity, CommunityRoles.MODERATOR);
                ManagedGroups = owned.Concat(moderated).Select(group => group.Id).ToHashSet();
            }
            else if (CanManageCommunity && Guid.TryParse(filter, out var managedId))
            {
                ManagedGroups.Add(managedId);
            }

            return base.Page();
        }
        
    }
}
