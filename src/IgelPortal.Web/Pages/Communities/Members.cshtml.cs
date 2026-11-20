using IgelPortal.Web.Interfaces;
using IgelPortal.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Communities
{
    [Authorize(Roles = $"{RoleNames.USER},{RoleNames.ADMIN}", Policy = PolicyNames.COMMUNITY_OWNER)]
    public class MembersModel(ICommunityDataService dataService) : PageModel
    {
        private readonly ICommunityDataService dataService = dataService;

        public IList<CommunityMembersModel> CommunityMembers { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (!id.HasValue)
            {
                return base.RedirectToPage("/Index");
            }

            Boolean isCommunityModerator = await this.dataService.HasUserCommunityMemberRole(id, base.User.Identity, CommunityRoles.MODERATOR);
            Boolean isCommunityOwner = await this.dataService.HasUserCommunityMemberRole(id, base.User.Identity, CommunityRoles.OWNER);

            if (!isCommunityModerator && !isCommunityOwner)
            {
                return base.RedirectToPage("/Index");
            }

            await GetCommunityMemberList(id.Value);

            return base.Page();

        }

        public async Task<IActionResult> OnPostAccept([FromRoute] Guid? id, [FromRoute] String? username)
        {
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.AcceptCommunityApplication(id, username);

            return RedirectToPage(new { id });
        }
        public async Task<IActionResult> OnPostReject([FromRoute] Guid? id, [FromRoute] String? username)
        {
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.RejectCommunityApplication(id, username);

            return RedirectToPage(new { id });
        }
        public async Task<IActionResult> OnPostPromote([FromRoute] Guid? id, [FromRoute] String? username)
        {
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.PromoteUserToCommunityModerator(id, username);

            return RedirectToPage(new { id });
        }
        public async Task<IActionResult> OnPostMakeOwner([FromRoute] Guid? id, [FromRoute] String? username)
        {
            if (!User.IsInRole(RoleNames.ADMIN))
            {
                return Forbid();
            }
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.AddUserAsOwner(id, username);

            return RedirectToPage(new { id });
        }
        public async Task<IActionResult> OnPostDemote([FromRoute] Guid? id, [FromRoute] String? username)
        {
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.DemoteUserFromCommunityModerator(id, username);

            return RedirectToPage(new { id });
        }
        public async Task<IActionResult> OnPostRemove([FromRoute] Guid? id, [FromRoute] String? username)
        {
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.RemoveUserFromCommunity(id, username);

            return RedirectToPage(new { id });
        }

        private async Task GetCommunityMemberList(Guid id)
        {
            Boolean isCommunityModerator = await this.dataService.HasUserCommunityMemberRole(id, base.User.Identity, CommunityRoles.MODERATOR);
            Boolean isCommunityOwner = await this.dataService.HasUserCommunityMemberRole(id, base.User.Identity, CommunityRoles.OWNER);
            base.ViewData["IsCommunityModerator"] = isCommunityModerator;
            base.ViewData["IsCommunityOwner"] = isCommunityOwner;

            this.CommunityMembers = [];
            var communityMembersList = await this.dataService.GetCommunityMembers(id, true);
            var groupedCommunityMembersList = communityMembersList.GroupBy(key => key.UserName).ToDictionary(key => key.Key, value => value.ToList());
            foreach (var communityMember in groupedCommunityMembersList)
            {
                CommunityMembersModel modelItem = new()
                {
                    UserName = communityMember.Key,
                    DisplayName = communityMember.Value[0].DisplayName,
                    CommunityId = communityMember.Value.First().CommunityId,
                    CommunityRoles = communityMember.Value.Select(c => c.CommunityRoleName).ToList()
                };
                this.CommunityMembers.Add(modelItem);
            }
        }
    }
}
