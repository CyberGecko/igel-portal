using IgelPortal.Web.Data;
using IgelPortal.Web.Data.Care;
using IgelPortal.Web.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace IgelPortal.Web.Pages;

public class IndexModel(IOptions<RequestLocalizationOptions> localization, ApplicationDbContext db,
    PlacementService placements, MedicationPlanService medication, CareClock clock) : PageModel
{
    public sealed record PlacementItem(Guid Id, Guid GroupId, Guid HedgehogId, Guid Token, string Hedgehog, string Service, string Group,
        bool HasReply, bool HasAvailability);
    public sealed record ApplicantGroup(Guid Id, string Name, int Count);

    public IReadOnlyList<CareTask> MedicationTasks { get; private set; } = [];
    public IReadOnlyList<PlacementOffer> OwnOffers { get; private set; } = [];
    public IReadOnlyList<PlacementItem> Incoming { get; private set; } = [];
    public IReadOnlyList<PlacementItem> OpenPlacements { get; private set; } = [];
    public IReadOnlyList<ApplicantGroup> Applicants { get; private set; } = [];
    public CareClock Clock => clock;
    public DateTime Now { get; private set; }
    public bool IsToday(DateTime utc) => clock.FromUtc(utc).Date == clock.FromUtc(Now).Date;

    public async Task OnGetAsync()
    {
        if (!User.IsInRole(RoleNames.USER) || User.Identity?.Name is not { } userName) return;

        Now = DateTime.UtcNow;
        MedicationTasks = (await medication.TasksAsync(User)).Take(5).ToList();
        OwnOffers = await placements.OffersAsync(groupId: null, owner: userName, freeOnly: false);
        var activeOffers = OwnOffers.Where(offer => offer.CommunityId.HasValue && !offer.Paused && offer.Free > 0)
            .Select(offer => (GroupId: offer.CommunityId!.Value, offer.ServiceId)).ToHashSet();
        var offerGroups = activeOffers.Select(offer => offer.GroupId).Distinct().ToArray();
        var memberships = db.CommunityMembers.Where(member => member.UserName == userName &&
            member.Community.DeletedDate == null);
        var managedGroups = memberships.Where(member => member.CommunityRoleName == CommunityRoles.OWNER ||
            member.CommunityRoleName == CommunityRoles.MODERATOR).Select(member => member.CommunityId);
        var open = placements.OpenRequests();
        OpenPlacements = await open.Where(request => managedGroups.Contains(request.CommunityId))
            .OrderByDescending(request => request.Responses.Any(reply => reply.IsAvailable))
            .ThenBy(request => request.NeededFrom == null).ThenBy(request => request.NeededFrom)
            .Take(8)
            .Select(request => new PlacementItem(request.Id, request.CommunityId, request.HedgehogId, request.RequestToken,
                request.Hedgehog.Name, request.Service.Name, request.Community.Name, request.Responses.Any(),
                request.Responses.Any(reply => reply.IsAvailable))).ToListAsync();

        var incoming = await open.Where(request =>
            memberships.Any(member => member.CommunityId == request.CommunityId && member.CommunityRoleName != CommunityRoles.APPLICANT) &&
            (offerGroups.Contains(request.CommunityId) || request.Responses.Any(reply => reply.RespondingUserName == userName)))
            .OrderBy(request => request.Responses.Any(reply => reply.RespondingUserName == userName))
            .ThenBy(request => request.NeededFrom == null).ThenBy(request => request.NeededFrom)
            .Select(request => new
            {
                request.ServiceId,
                Item = new PlacementItem(request.Id, request.CommunityId, request.HedgehogId, request.RequestToken,
                    request.Hedgehog.Name, request.Service.Name, request.Community.Name,
                    request.Responses.Any(reply => reply.RespondingUserName == userName),
                    request.Responses.Any(reply => reply.RespondingUserName == userName && reply.IsAvailable))
            }).ToListAsync();
        Incoming = incoming.Where(request => request.Item.HasReply || activeOffers.Contains((request.Item.GroupId, request.ServiceId)))
            .Take(8).Select(request => request.Item).ToList();

        var ownedGroups = memberships.Where(member => member.CommunityRoleName == CommunityRoles.OWNER)
            .Select(member => member.CommunityId);
        Applicants = await db.CommunityMembers.Where(member => member.CommunityRoleName == CommunityRoles.APPLICANT &&
                ownedGroups.Contains(member.CommunityId))
            .GroupBy(member => new { member.CommunityId, member.Community.Name })
            .Select(group => new { group.Key.CommunityId, group.Key.Name, Count = group.Count() })
            .OrderBy(group => group.Name)
            .Select(group => new ApplicantGroup(group.CommunityId, group.Name, group.Count)).ToListAsync();
    }

    public IActionResult OnPostSetLanguage(string? culture, string? returnUrl)
    {
        var supported = localization.Value.SupportedUICultures?
            .FirstOrDefault(item => string.Equals(item.Name, culture, StringComparison.OrdinalIgnoreCase));
        if (supported is not null)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(supported)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true
                });
        }

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToPage("/Index");
    }
}
