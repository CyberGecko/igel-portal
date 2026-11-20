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

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER, Policy = PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)]
    public class RequestServiceModel(IHedgehogDataService dataService, ICommunityDataService communityDataService, ITerritoryDataService territoryDataService, ITerritoryServiceDataService territoryServiceDataService,
                                     IServiceRequestDataService serviceRequestDataService, UserManager<ApplicationUser> userManager, IStringLocalizer<RequestServiceModel> localizer, IEmailSender emailSender) : PageModel
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

        public async Task OnGet(Guid? id, Guid? hedgehogId)
        {
            this.ServiceRequests = await this.serviceRequestDataService.FindAllActiveForCommunityAndHedgehog(id, hedgehogId);

            var territories = await this.territoryDataService.FindAllActiveForCommunity(id);
            var communityServices = territories.SelectMany(c => c.AttachedServices.Where(s => s.CurrentCapacity > 0));

            var hedgehog = await this.dataService.FindOne(hedgehogId);

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
        }

        public async Task<IActionResult> OnGetServiceResponse(Guid? requestToken)
        {
            var serviceRequest = await this.serviceRequestDataService.FindOneByToken(requestToken);
            if (serviceRequest != null)
            {
                await this.serviceRequestDataService.AddResponseToRequest(serviceRequest.Id, base.User.Identity);
            }

            return base.RedirectToPage("/");
        }

        public async Task<IActionResult> OnGetMarkAsFulfilled(Guid? id)
        {
            var serviceRequest = await this.serviceRequestDataService.FindOne(id);
            if (serviceRequest == null)
            {
                return base.RedirectToPage("/");
            }
            serviceRequest.FulfillmentDate = DateTime.UtcNow;

            await this.serviceRequestDataService.Update(serviceRequest);
            return base.RedirectToPage(new { id = serviceRequest.CommunityId, serviceRequest.HedgehogId });

        }

        public async Task<IActionResult> OnGetInitiateAction(Guid? id, Guid? territoryId)
        {
            ServiceRequest? serviceRequest = await this.serviceRequestDataService.FindOne(id);
            Territory? territory = await this.territoryDataService.FindOne(territoryId);
            if (serviceRequest == null || territory == null)
            {
                return base.RedirectToPage("/");
            }
            Guid communityId = serviceRequest.CommunityId;
            Guid hedgehogId = serviceRequest.HedgehogId;
            Guid serviceId = serviceRequest.ServiceId;

            TerritoryAttachedService? attachedService = territory.AttachedServices.FirstOrDefault(c => c.TerritoryServiceId == serviceId);
            Hedgehog? currentHedgehog = await this.dataService.FindOne(hedgehogId);
            if (currentHedgehog != null && attachedService != null)
            {
                currentHedgehog.CurrentTerritoryId = territoryId;
                currentHedgehog.UsedServiceId = attachedService.Id;
                await this.dataService.Update(currentHedgehog);
            }

            return base.RedirectToPage(new { id = serviceRequest.CommunityId, serviceRequest.HedgehogId });
        }

        public async Task<IActionResult> OnPost(Guid? hedgehogId, Guid? communityId, Guid? requestServiceId)
        {
            if (hedgehogId.HasValue && communityId.HasValue && requestServiceId.HasValue)
            {
                var community = await this.communityDataService.FindOne(communityId.Value);
                var hedgehog = await this.dataService.FindOne(hedgehogId.Value);
                var service = await this.territoryServiceDataService.FindOne(requestServiceId.Value);
                var territories = await this.territoryDataService.FindAllForCommunityWithService(communityId.Value, requestServiceId.Value);

                if (hedgehog != null && community != null && service != null && territories.Count > 0)
                {
                    ServiceRequest serviceRequest = new()
                    {
                        CommunityId = communityId.Value,
                        ServiceId = requestServiceId.Value,
                        HedgehogId = hedgehogId.Value,
                    };

                    await this.serviceRequestDataService.Insert(serviceRequest);

                    String serviceResponseUrl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.RouteValues["page"]}/ServiceResponse";
                    String subject = this.localizer.GetString("Hedgehog Portal service request");
                    String body = this.localizer.GetString("Service Request Mail Text", community.Name, service.Name, hedgehog.Name, serviceResponseUrl, serviceRequest.RequestToken);
                    List<Task> tasks = [];

                    List<String?> mailAddresses = territories.SelectMany(c => userManager.Users.Where(c => c.UserName == territories[0].UserName)).Select(c => c.Email).Distinct().ToList();
                    foreach (var mailAddress in mailAddresses.Where(c => c != null))
                    {
                        if (mailAddress != null)
                        {
                            tasks.Add(Task.Run(() => emailSender.SendEmailAsync(mailAddress, subject, body)));
                        }
                    }

                    await Task.WhenAll(tasks);
                }
            }
            return base.RedirectToPage(new { id = communityId, hedgehogId });
        }
    }
}
