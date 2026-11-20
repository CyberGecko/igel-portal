using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IgelPortal.Web.ViewComponents
{
    [ViewComponent(Name = "HedgehogOverviewWidget")]
    public class HedgehogOverviewWidgetViewComponent(IHedgehogDataService dataService) : ViewComponent
    {
        private readonly IHedgehogDataService dataService = dataService;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            IEnumerable<Hedgehog> hedgehogs = await this.dataService.FindAllActiveForUser(User?.Identity?.Name);
            return base.View(hedgehogs);
        }
    }
}
