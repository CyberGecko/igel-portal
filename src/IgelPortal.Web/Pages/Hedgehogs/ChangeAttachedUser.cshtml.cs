using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class ChangeAttachedUserModel(IHedgehogDataService dataService, ICommunityDataService communityDataService, IAuthorizationService authorization) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;
        private readonly ICommunityDataService communityDataService = communityDataService;

        public SelectList AttachableUser { get; set; } = default!;

        public async Task<IActionResult> OnGet(Guid? id, Guid? hedgehogId)
        {
            if (!await CanManage(id))
            {
                return Forbid();
            }

            var hedgehog = await dataService.FindOne(hedgehogId);
            if (hedgehog is null || hedgehog.IsDeleted || hedgehog.CommunityId != id)
            {
                return NotFound();
            }

            var communityMembers = await this.communityDataService.GetCommunityMembers(id, false);


            this.AttachableUser = new SelectList(communityMembers.OrderBy(c => c.DisplayName).DistinctBy(c => c.UserName), "UserName", "DisplayName");
            base.ViewData["CurrentAttachedUser"] = hedgehog?.AttachedUserDisplayName;
            base.ViewData["HedgehogId"] = hedgehogId;
            base.ViewData["CommunityId"] = id;
            return Page();
        }

        public async Task<IActionResult> OnPost(Guid? hedgehogId, String? newAttachedUserName, Guid? communityId)
        {
            if (!await CanManage(communityId))
            {
                return Forbid();
            }
            if (!hedgehogId.HasValue || String.IsNullOrEmpty(newAttachedUserName))
            {
                return base.RedirectToPage();
            }
            Hedgehog? currentHedgehog = await this.dataService.FindOne(hedgehogId.Value);

            if (currentHedgehog is null || currentHedgehog.IsDeleted || currentHedgehog.CommunityId != communityId)
            {
                return NotFound();
            }
            if (!await communityDataService.IsCommunityMember(communityId, new System.Security.Principal.GenericIdentity(newAttachedUserName)))
            {
                return BadRequest();
            }

            currentHedgehog.AttachedUser = newAttachedUserName;
            await dataService.Update(currentHedgehog);

            return base.RedirectToPage("./Index", new { filter = communityId });
        }
        private async Task<bool> CanManage(Guid? communityId) =>
            communityId.HasValue && (await authorization.AuthorizeAsync(User, communityId.Value, PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)).Succeeded;
    }
}
