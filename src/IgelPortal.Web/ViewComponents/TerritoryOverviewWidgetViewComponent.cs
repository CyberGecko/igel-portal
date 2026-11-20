using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IgelPortal.Web.ViewComponents
{
    [ViewComponent(Name = "TerritoryOverviewWidget")]
    public class TerritoryOverviewWidgetViewComponent(ITerritoryDataService dataService) : ViewComponent
    {
        private readonly ITerritoryDataService dataService = dataService;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            IEnumerable<Territory> territories = await this.dataService.FindAllActiveForUser(base.User.Identity);

            return base.View(territories);
        }
    }
}
