using IgelPortal.Web.Data.Care;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Data.Services;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using IgelPortal.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Hedgehogs;

[Authorize(Roles = RoleNames.USER)]
public class RequestServiceModel(IHedgehogDataService hedgehogs, ICommunityDataService communities,
    IServiceRequestDataService requests, PlacementService placements, IAuthorizationService authorization,
    PortalMail email, ILogger<RequestServiceModel> logger,
    CareClock clock) : PageModel
{
    public Hedgehog Hedgehog { get; private set; } = default!;
    public ServiceRequest? ResponseRequest { get; private set; }
    public ServiceRequestResponse? MyReply { get; private set; }
    public string GroupName { get; private set; } = "";
    public IReadOnlyList<ServiceRequest> Requests { get; private set; } = [];
    public IReadOnlyList<PlacementOffer> Offers { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name, int Free, int Recipients)> Services { get; private set; } = [];
    public bool CanReply { get; private set; }
    public string Today => DateOnly.FromDateTime(clock.LocalNow).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    [BindProperty]
    public Guid? ServiceId { get; set; }

    [BindProperty]
    public DateOnly? NeededFrom { get; set; }

    [BindProperty]
    public DateOnly? NeededUntil { get; set; }

    [BindProperty]
    public string? Note { get; set; }

    [TempData]
    public string? PlacementNotice { get; set; }

    [TempData]
    public int ServiceRequestMailFailures { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, Guid hedgehogId)
    {
        var result = await LoadRequestPageAsync(id, hedgehogId);
        if (result is PageResult)
        {
            var reachable = Services.Where(service => service.Recipients > 0).Take(2).ToArray();
            if (reachable.Length == 1)
            {
                ServiceId = reachable[0].Id;
            }
        }
        return result;
    }

    private async Task<IActionResult> LoadRequestPageAsync(Guid id, Guid hedgehogId)
    {
        if (!await CanManage(id)) return Forbid();
        var hedgehog = await hedgehogs.FindOne(hedgehogId);
        var group = await communities.FindOne(id);
        if (hedgehog is null || hedgehog.IsDeleted || hedgehog.CommunityId != id || group is null || group.IsDeleted)
            return NotFound();

        Hedgehog = hedgehog;
        GroupName = group.Name;
        Requests = (await requests.FindAllActiveForCommunityAndHedgehog(id, hedgehogId)).ToList();
        Offers = await placements.OffersAsync(id);
        var recipients = await placements.ConfirmedRecipientsAsync(Offers);
        Services = Offers.GroupBy(offer => (offer.ServiceId, offer.ServiceName))
            .Select(group => (group.Key.ServiceId, group.Key.ServiceName, group.Sum(offer => offer.Free),
                group.Select(offer => recipients.GetValueOrDefault(offer.UserName)).OfType<string>().Distinct().Count()))
            .OrderBy(item => item.ServiceName).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostRequestServiceAsync(Guid communityId, Guid hedgehogId)
    {
        if (!await CanManage(communityId)) return Forbid();
        var hedgehog = await hedgehogs.FindOne(hedgehogId);
        var group = await communities.FindOne(communityId);
        if (hedgehog is null || hedgehog.IsDeleted || hedgehog.CommunityId != communityId || group is null || group.IsDeleted)
            return NotFound();

        Note = Note?.Trim();
        if (!ModelState.IsValid || ServiceId is null || NeededFrom is null ||
            NeededFrom < DateOnly.FromDateTime(clock.LocalNow) || NeededUntil < NeededFrom || Note?.Length > 1000)
        {
            PlacementNotice = "Check the service, dates and note.";
            return await LoadRequestPageAsync(communityId, hedgehogId);
        }

        var offers = await placements.OffersAsync(communityId, ServiceId);
        var recipients = (await placements.ConfirmedRecipientsAsync(offers)).Values.Distinct().ToList();
        if (recipients.Count == 0)
        {
            PlacementNotice = "There are no free offers with a reachable provider for this service.";
            return await LoadRequestPageAsync(communityId, hedgehogId);
        }
        if ((await requests.FindAllActiveForCommunityAndHedgehog(communityId, hedgehogId))
            .Any(request => request.ServiceId == ServiceId && !request.IsFulfilled))
        {
            PlacementNotice = "There is already an open request for this service.";
            return RedirectToPage(new { id = communityId, hedgehogId });
        }

        var request = new ServiceRequest
        {
            CommunityId = communityId, ServiceId = ServiceId.Value, HedgehogId = hedgehogId,
            NeededFrom = NeededFrom, NeededUntil = NeededUntil, RequestNote = string.IsNullOrEmpty(Note) ? null : Note
        };
        var responseUrl = Url.PageLink("./RequestService", "ServiceResponse", new { requestToken = request.RequestToken }, Request.Scheme)
            ?? throw new InvalidOperationException("Could not generate the service-response URL.");
        await requests.Insert(request);

        var failures = 0;
        await Parallel.ForEachAsync(recipients, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (recipient, _) =>
        {
            if (!await TryEmailAsync(() => email.PlacementRequestAsync(recipient, group.Name, hedgehog.Name,
                offers[0].ServiceName, NeededFrom.Value, NeededUntil, responseUrl)))
                Interlocked.Increment(ref failures);
        });
        ServiceRequestMailFailures = failures;
        PlacementNotice = "Placement request sent.";
        return RedirectToPage(new { id = communityId, hedgehogId });
    }

    public async Task<IActionResult> OnGetServiceResponseAsync(Guid requestToken)
    {
        var request = await FindRespondableRequest(requestToken);
        if (request is null) return NotFound();
        Response.Headers["Referrer-Policy"] = "no-referrer";
        ResponseRequest = request;
        GroupName = (await communities.FindOne(request.CommunityId))?.Name ?? "";
        MyReply = request.Responses.SingleOrDefault(reply => reply.RespondingUserName == User.Identity?.Name);
        var ownOffers = await placements.OffersAsync(request.CommunityId, request.ServiceId,
            owner: User.Identity!.Name!, freeOnly: false);
        CanReply = !request.IsFulfilled && (ownOffers.Count != 0 || MyReply is not null);
        Offers = ownOffers.Where(offer => !offer.Paused && offer.Free > 0).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostServiceResponseAsync(Guid requestToken, PlacementReply? reply, Guid? offerId)
    {
        var request = await FindRespondableRequest(requestToken);
        if (request is null) return NotFound();
        var saved = ModelState.IsValid && reply.HasValue &&
            await placements.ReplyAsync(request.Id, User.Identity!.Name!, reply.Value, offerId);
        PlacementNotice = saved
            ? (reply == PlacementReply.Withdraw ? "Your reply was withdrawn." : "Your reply was saved.")
            : "The reply could not be saved. Check whether the request is still open and the offer is available.";
        return RedirectToPage(pageName: null, pageHandler: "ServiceResponse", routeValues: new { requestToken });
    }

    public async Task<IActionResult> OnPostAssignAsync(Guid id, Guid offerId)
    {
        var request = await requests.FindOne(id);
        if (request is null || request.IsDeleted) return NotFound();
        if (!await CanManage(request.CommunityId)) return Forbid();

        var assignment = await placements.AssignAsync(id, offerId, User.Identity!.Name!);
        PlacementNotice = assignment is null ? "The place is no longer available or the reply changed." : "Place assigned.";
        if (assignment?.Email is { Length: > 0 } recipient)
        {
            var responseUrl = Url.PageLink("./RequestService", "ServiceResponse", new { requestToken = request.RequestToken }, Request.Scheme)
                ?? throw new InvalidOperationException("Could not generate the service-response URL.");
            if (!await TryEmailAsync(() => email.PlacementAssignedAsync(recipient, assignment.Hedgehog, assignment.Service,
                assignment.Territory, responseUrl)))
                ServiceRequestMailFailures = 1;
        }
        return RedirectToPage(new { id = request.CommunityId, hedgehogId = request.HedgehogId });
    }

    public async Task<IActionResult> OnPostMarkAsFulfilledAsync(Guid id)
    {
        var request = await requests.FindOne(id);
        if (request is null || request.IsDeleted) return NotFound();
        if (!await CanManage(request.CommunityId)) return Forbid();

        if (await placements.CloseAsync(id))
            PlacementNotice = "Request closed.";
        return RedirectToPage(new { id = request.CommunityId, hedgehogId = request.HedgehogId });
    }

    private async Task<ServiceRequest?> FindRespondableRequest(Guid token)
    {
        var request = await requests.FindOneByToken(token);
        if (request is null || request.IsDeleted || !await communities.IsCommunityMember(request.CommunityId, User.Identity))
            return null;
        var hedgehog = await hedgehogs.FindOne(request.HedgehogId);
        return hedgehog is { IsDeleted: false } && hedgehog.CommunityId == request.CommunityId ? request : null;
    }

    private async Task<bool> TryEmailAsync(Func<Task> send)
    {
        try
        {
            await send();
            return true;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Could not submit a placement notification.");
            return false;
        }
    }

    private async Task<bool> CanManage(Guid communityId) =>
        communityId != Guid.Empty && (await authorization.AuthorizeAsync(User, communityId, PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)).Succeeded;
}
