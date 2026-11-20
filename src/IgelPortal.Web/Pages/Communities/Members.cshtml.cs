using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Interfaces;
using IgelPortal.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Communities
{
    [Authorize(Roles = $"{RoleNames.USER},{RoleNames.ADMIN}", Policy = PolicyNames.COMMUNITY_OWNER)]
    public class MembersModel(ICommunityDataService dataService, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ICommunityDataService dataService = dataService;
        private readonly UserManager<ApplicationUser> userManager = userManager;

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

        public async Task<IActionResult> OnGetAccept(Guid? id, String? username)
        {
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.AcceptCommunityApplication(id, username);

            await GetCommunityMemberList(id.Value);
            return base.Page();
        }
        public async Task<IActionResult> OnGetReject(Guid? id, String? username)
        {
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.RejectCommunityApplication(id, username);

            await GetCommunityMemberList(id.Value);
            return base.Page();
        }
        public async Task<IActionResult> OnGetPromote(Guid? id, String? username)
        {
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.PromoteUserToCommunityModerator(id, username);

            await GetCommunityMemberList(id.Value);
            return base.Page();
        }
        public async Task<IActionResult> OnGetMakeOwner(Guid? id, String? username)
        {
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.AddUserAsOwner(id, username);

            await GetCommunityMemberList(id.Value);
            return base.Page();
        }
        public async Task<IActionResult> OnGetDemote(Guid? id, String? username)
        {
            if (!id.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.DemoteUserFromCommunityModerator(id, username);

            await GetCommunityMemberList(id.Value);
            return base.Page();
        }
        public async Task<IActionResult> OnPost(Guid? communityId, String? username)
        {
            if (!communityId.HasValue || String.IsNullOrEmpty(username))
            {
                return base.RedirectToPage("/Index");
            }

            await this.dataService.RemoveUserFromCommunity(communityId, username);

            await GetCommunityMemberList(communityId.Value);
            return base.Page();
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
                ApplicationUser? user = this.userManager.Users.FirstOrDefault(c => c.UserName == communityMember.Key);
                CommunityMembersModel modelItem = new()
                {
                    UserName = communityMember.Key,
                    DisplayName = user?.ToString() ?? communityMember.Key,
                    CommunityId = communityMember.Value.First().CommunityId,
                    CommunityRoles = communityMember.Value.Select(c => c.CommunityRoleName).ToList()
                };
                this.CommunityMembers.Add(modelItem);
            }
        }
    }
}
