using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using IgelPortal.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Communities;

[Authorize(Roles = RoleNames.USER)]
public class JoinModel(ICommunityDataService communities, PortalMail emailSender, UserManager<ApplicationUser> users, ILogger<JoinModel> logger) : PageModel
{
    public IReadOnlyList<Community> Managed { get; private set; } = [];
    public IReadOnlyList<Community> Joined { get; private set; } = [];
    public IReadOnlyList<Community> Pending { get; private set; } = [];
    public IReadOnlyList<Community> Available { get; private set; } = [];
    public IReadOnlySet<Guid> OwnedIds { get; private set; } = new HashSet<Guid>();

    public async Task OnGetAsync()
    {
        var identity = User.Identity;
        var owned = await communities.FindAllForUserWithRole(identity, CommunityRoles.OWNER);
        var moderated = await communities.FindAllForUserWithRole(identity, CommunityRoles.MODERATOR);
        OwnedIds = owned.Select(community => community.Id).ToHashSet();
        Managed = owned.Concat(moderated).DistinctBy(community => community.Id).OrderBy(community => community.Name).ToList();

        var managedIds = Managed.Select(community => community.Id).ToHashSet();
        Joined = (await communities.FindAllForUserWithRole(identity, CommunityRoles.MEMBER))
            .Where(community => !managedIds.Contains(community.Id)).ToList();
        Pending = (await communities.FindAllForUserWithRole(identity, CommunityRoles.APPLICANT)).ToList();

        var associatedIds = Managed.Concat(Joined).Concat(Pending).Select(community => community.Id).ToHashSet();
        Available = (await communities.FindAll()).Where(community => !associatedIds.Contains(community.Id)).ToList();
    }

    public async Task<IActionResult> OnPostAsync(Guid? communityId)
    {
        var user = await users.GetUserAsync(User);
        var community = await communities.FindOne(communityId);
        if (user is null || community is null || community.IsDeleted)
        {
            return NotFound();
        }

        if (!await communities.TryAddUserAsApplicant(community.Id, User.Identity))
        {
            TempData["CommunityApplicationNotice"] = "You already belong to this community or have requested to join it.";
            return RedirectToPage();
        }

        TempData["CommunityApplicationNotice"] = "Your request was sent to the community.";
        if (!string.IsNullOrWhiteSpace(community.ContactMail))
        {
            var membersUrl = Url.PageLink("/Communities/Members", values: new { id = community.Id });
            if (membersUrl is not null)
            {
                try
                {
                    await emailSender.JoinRequestAsync(community.ContactMail, user.ToString(), community.Name, membersUrl);
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
                {
                    logger.LogWarning("Could not email the owner about an application for community {CommunityId}.", community.Id);
                    TempData["CommunityApplicationNotice"] = "Your request was saved. The email notification could not be sent.";
                }
            }
        }

        return RedirectToPage();
    }
}
