using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER, Policy = PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)]
    public class ChangeAttachedUserModel(IHedgehogDataService dataService, ICommunityDataService communityDataService) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;
        private readonly ICommunityDataService communityDataService = communityDataService;

        public SelectList AttachableUser { get; set; } = default!;

        public async Task OnGet(Guid? id, Guid? hedgehogId)
        {
            var communityMembers = await this.communityDataService.GetCommunityMembers(id, false);

            Hedgehog? hedgehog = hedgehogId.HasValue ? await this.dataService.FindOne(hedgehogId.Value) : null;

            this.AttachableUser = new SelectList(communityMembers.OrderBy(c => c.DisplayName).DistinctBy(c => c.UserName), "UserName", "DisplayName");
            base.ViewData["CurrentAttachedUser"] = hedgehog?.AttachedUserDisplayName;
            base.ViewData["HedgehogId"] = hedgehogId;
            base.ViewData["CommunityId"] = id;
        }

        public async Task<IActionResult> OnPost(Guid? hedgehogId, String? newAttachedUserName, Guid? communityId)
        {
            if (!hedgehogId.HasValue || String.IsNullOrEmpty(newAttachedUserName))
            {
                return base.RedirectToPage();
            }
            Hedgehog? currentHedgehog = await this.dataService.FindOne(hedgehogId.Value);

            if (currentHedgehog != null)
            {
                currentHedgehog.AttachedUser = newAttachedUserName;
                await this.dataService.Update(currentHedgehog);
            }

            return base.RedirectToPage("./Index", new { filter = communityId });
        }
    }
}
