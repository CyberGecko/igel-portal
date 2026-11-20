using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Syncfusion.EJ2.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class RequestServiceModel(IHedgehogDataService dataService, ICommunityDataService communityDataService, ITerritoryDataService territoryDataService, ITerritoryServiceDataService territoryServiceDataService,
                                     IServiceRequestDataService serviceRequestDataService, UserManager<ApplicationUser> userManager, IStringLocalizer<RequestServiceModel> localizer, IEmailSender emailSender, IAuthorizationService authorization, ILogger<RequestServiceModel> logger) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;
        private readonly ICommunityDataService communityDataService = communityDataService;
        private readonly ITerritoryDataService territoryDataService = territoryDataService;
        private readonly ITerritoryServiceDataService territoryServiceDataService = territoryServiceDataService;
        private readonly IServiceRequestDataService serviceRequestDataService = serviceRequestDataService;
        private readonly UserManager<ApplicationUser> userManager = userManager;
        private readonly IStringLocalizer<RequestServiceModel> localizer = localizer;
        private readonly IEmailSender emailSender = emailSender;

        public IList<ServiceRequest> ServiceRequests { get; set; } = default!;

        public Guid? ResponseToken { get; private set; }

        public async Task<IActionResult> OnGet(Guid? id, Guid? hedgehogId)
        {
            if (!await CanManage(id))
            {
                return Forbid();
            }

            var hedgehog = await this.dataService.FindOne(hedgehogId);
            if (hedgehog is null || hedgehog.IsDeleted || hedgehog.CommunityId != id)
            {
                return NotFound();
            }

            this.ServiceRequests = await this.serviceRequestDataService.FindAllActiveForCommunityAndHedgehog(id, hedgehogId);

            var territories = await this.territoryDataService.FindAllActiveForCommunity(id);
            var communityServices = territories.SelectMany(c => c.AttachedServices.Where(s => s.CurrentCapacity > 0));

            Dictionary<Guid, Dictionary<String, List<Territory>>> availableServiceTerritories = [];
            this.ServiceRequests.ForEach(request =>
            {
                availableServiceTerritories.Add(request.Id, []);
                territories.Where(t => t.AttachedServices.Any(a => a.TerritoryServiceId == request.ServiceId && a.CurrentCapacity > 0))
                           .DistinctBy(c => c.Id)
                           .ForEach(territory =>
                           {
                               availableServiceTerritories[request.Id].TryAdd(territory.UserName, []);
                               availableServiceTerritories[request.Id][territory.UserName].Add(territory);
                           });
            });


            base.ViewData["RequestableServices"] = new SelectList(communityServices.OrderBy(c => c.TerritoryService).DistinctBy(c => c.TerritoryServiceId), "TerritoryServiceId", "TerritoryService");
            base.ViewData["HedgehogId"] = hedgehogId;
            base.ViewData["HedgehogName"] = hedgehog?.Name ?? "n.a.";
            base.ViewData["CommunityId"] = id;
            base.ViewData["UserTerritories"] = availableServiceTerritories;
            return Page();
        }

        public async Task<IActionResult> OnGetServiceResponse(Guid? requestToken)
        {
            if (await FindRespondableRequest(requestToken) is null)
            {
                return NotFound();
            }

            Response.Headers["Referrer-Policy"] = "no-referrer";
            ResponseToken = requestToken;
            return Page();
        }

        public async Task<IActionResult> OnPostServiceResponse(Guid? requestToken)
        {
            var request = await FindRespondableRequest(requestToken);
            if (request is null)
            {
                return NotFound();
            }

            await serviceRequestDataService.AddResponseToRequest(request.Id, User.Identity);
            return RedirectToPage("/Index");
        }

        private async Task<ServiceRequest?> FindRespondableRequest(Guid? token)
        {
            if (token is null)
            {
                return null;
            }

            var request = await serviceRequestDataService.FindOneByToken(token);
            if (request is null || request.IsDeleted || request.IsFulfilled)
            {
                return null;
            }

            return await communityDataService.IsCommunityMember(request.CommunityId, User.Identity) ? request : null;
        }

        public async Task<IActionResult> OnPostMarkAsFulfilled(Guid? id)
        {
            var request = await serviceRequestDataService.FindOne(id);
            if (request is null || request.IsDeleted)
            {
                return NotFound();
            }
            if (!await CanManage(request.CommunityId))
            {
                return Forbid();
            }

            if (!request.IsFulfilled)
            {
                request.FulfillmentDate = DateTime.UtcNow;
                await serviceRequestDataService.Update(request);
            }
            return RedirectToPage(new { id = request.CommunityId, request.HedgehogId });
        }

        public async Task<IActionResult> OnPostInitiateAction(Guid? id, Guid? territoryId)
        {
            var request = await serviceRequestDataService.FindOne(id);
            if (request is null || request.IsDeleted || request.IsFulfilled)
            {
                return NotFound();
            }
            if (!await CanManage(request.CommunityId))
            {
                return Forbid();
            }

            var territory = await territoryDataService.FindOne(territoryId);
            var hedgehog = await dataService.FindOne(request.HedgehogId);
            if (territory is null || territory.IsDeleted || territory.CommunityId != request.CommunityId ||
                hedgehog is null || hedgehog.IsDeleted || hedgehog.CommunityId != request.CommunityId ||
                !request.Responses.Any(response => response.RespondingUserName == territory.UserName))
            {
                return NotFound();
            }

            var service = territory.AttachedServices.FirstOrDefault(item => item.TerritoryServiceId == request.ServiceId && item.CurrentCapacity > 0);
            if (service is null)
            {
                return BadRequest();
            }

            hedgehog.CurrentTerritoryId = territory.Id;
            hedgehog.UsedServiceId = service.Id;
            await dataService.Update(hedgehog);
            return RedirectToPage(new { id = request.CommunityId, request.HedgehogId });
        }

        public async Task<IActionResult> OnPostRequestService(Guid? hedgehogId, Guid? communityId, Guid? requestServiceId)
        {
            if (!await CanManage(communityId))
            {
                return Forbid();
            }
            if (!hedgehogId.HasValue || !communityId.HasValue || !requestServiceId.HasValue)
            {
                return BadRequest();
            }

            var community = await communityDataService.FindOne(communityId);
            var hedgehog = await dataService.FindOne(hedgehogId);
            var service = await territoryServiceDataService.FindOne(requestServiceId);
            if (community is null || community.IsDeleted ||
                hedgehog is null || hedgehog.IsDeleted || hedgehog.CommunityId != communityId ||
                service is null || service.IsDeleted)
            {
                return NotFound();
            }

            var territories = await territoryDataService.FindAllForCommunityWithService(communityId.Value, requestServiceId.Value);
            var userNames = territories.Where(territory => !territory.IsDeleted).Select(territory => territory.UserName).Distinct().ToArray();
            var emails = await userManager.Users.AsNoTracking()
                .Where(user => user.UserName != null && userNames.Contains(user.UserName) && user.EmailConfirmed &&
                               user.Email != null && user.Email != "")
                .Select(user => user.Email!).Distinct().ToListAsync(HttpContext.RequestAborted);
            if (emails.Count == 0)
            {
                return RedirectToPage(new { id = communityId, hedgehogId });
            }

            var request = new ServiceRequest { CommunityId = communityId.Value, ServiceId = requestServiceId.Value, HedgehogId = hedgehogId.Value };
            var responseUrl = Url.PageLink("/Hedgehogs/RequestService", "ServiceResponse", protocol: Request.Scheme)
                ?? throw new InvalidOperationException("Could not generate the service-response URL.");
            var encode = HtmlEncoder.Default;
            var subject = localizer.GetString("Hedgehog Portal service request");
            var body = localizer.GetString("Service Request Mail Text", encode.Encode(community.Name), encode.Encode(service.Name),
                encode.Encode(hedgehog.Name), encode.Encode(responseUrl), request.RequestToken);

            var requestId = await serviceRequestDataService.Insert(request);
            var failures = 0;
            await Parallel.ForEachAsync(emails, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (email, _) =>
            {
                try
                {
                    await emailSender.SendEmailAsync(email, subject, body);
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
                {
                    Interlocked.Increment(ref failures);
                }
            });
            if (failures > 0)
            {
                logger.LogWarning("Could not submit {FailedCount} email notifications for service request {RequestId}.", failures, requestId);
                TempData["ServiceRequestMailFailures"] = failures;
            }
            return RedirectToPage(new { id = communityId, hedgehogId });
        }

        private async Task<bool> CanManage(Guid? communityId) =>
            communityId.HasValue && (await authorization.AuthorizeAsync(User, communityId.Value, PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)).Succeeded;
    }
}
