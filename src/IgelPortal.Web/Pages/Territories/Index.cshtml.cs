using IgelPortal.Web.Data.Services;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Syncfusion.EJ2.Linq;

namespace IgelPortal.Web.Pages.Territories
{
    [Authorize(Roles = RoleNames.USER)]
    public class IndexModel(ITerritoryDataService dataService, ICommunityDataService communityDataService, PlacementService placements) : PageModel
    {
        private readonly ITerritoryDataService dataService = dataService;
        private readonly ICommunityDataService communityDataService = communityDataService;

        public IList<Territory> Territories { get; set; } = default!;

        public bool ShowMap { get; private set; }
        public string? Search { get; private set; }
        public bool FreeOnly { get; private set; }
        public IReadOnlyList<PlacementOffer> Offers { get; private set; } = [];

        public async Task<IActionResult> OnGetAsync(String? filter, bool map, string? search, bool freeOnly)
        {
            base.ViewData["Filter"] = filter;
            if (filter is null or "my")
            {
                this.Territories = await this.dataService.FindAllActiveForUser(User.Identity);

                base.ViewData["IsCommunityOverview"] = false;
                return base.Page();
            }
            else if (Guid.TryParse(filter, out Guid communityId))
            {
                if (!User.IsInRole(RoleNames.ADMIN) && !await communityDataService.IsCommunityMember(communityId, User.Identity))
                {
                    return Forbid();
                }
                base.ViewData["IsCommunityOverview"] = true;
                ShowMap = map;
                if (!ShowMap)
                {
                    Search = search?.Trim();
                    FreeOnly = freeOnly;
                    var offers = await placements.OffersAsync(communityId, freeOnly: freeOnly);
                    Offers = offers.Where(offer => string.IsNullOrWhiteSpace(Search) ||
                            offer.ServiceName.Contains(Search, StringComparison.CurrentCultureIgnoreCase) ||
                            offer.TerritoryName.Contains(Search, StringComparison.CurrentCultureIgnoreCase) ||
                            offer.City.Contains(Search, StringComparison.CurrentCultureIgnoreCase))
                        .OrderByDescending(offer => !offer.Paused && offer.Free > 0).ThenBy(offer => offer.ServiceName).ToList();
                    return Page();
                }

                this.Territories = await this.dataService.FindAllActiveForCommunity(communityId);

                base.ViewData["MiddleLat"] = this.Territories.Select(c => c.Latitude).DefaultIfEmpty().Average();
                base.ViewData["MiddleLon"] = this.Territories.Select(c => c.Longitude).DefaultIfEmpty().Average();
                base.ViewData["Data"] = this.Territories.Select(c => new
                {
                    latitude = c.Latitude,
                    longitude = c.Longitude,
                    name = c.Name,
                    services = c.AttachedServices.Select(s => new
                    {
                        name = s.TerritoryService,
                        currentCapacity = s.CurrentCapacity,
                        maxCapacity = s.MaxCapacity
                    })
                });

                return base.Page();
            }
            else
            {
                return new RedirectToPageResult("/Index");
            }
        }
    }
}
