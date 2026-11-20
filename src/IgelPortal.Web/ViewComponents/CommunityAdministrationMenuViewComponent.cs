using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using IgelPortal.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.ViewComponents
{
    [ViewComponent(Name = "CommunityAdministrationMenu")]
    public class CommunityAdministrationMenuViewComponent(ICommunityDataService dataService) : ViewComponent
    {
        private readonly ICommunityDataService dataService = dataService;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            List<Community> administeredCommunities = [.. await this.dataService.FindAllForUserWithRole(base.User.Identity, CommunityRoles.OWNER)];
            administeredCommunities.AddRange(await this.dataService.FindAllForUserWithRole(base.User.Identity, CommunityRoles.MODERATOR));

            IList<Community> myCommunities = await this.dataService.FindAllForUserWithRole(base.User.Identity, CommunityRoles.MEMBER);
            IList<Community> appliedCommunities = await this.dataService.FindAllForUserWithRole(base.User.Identity, CommunityRoles.APPLICANT);
            IList<Community> allCommunities = await this.dataService.FindAll();

            return base.View(new CommunityAdministrationMenuModel()
            {
                AdministratedCommunities = administeredCommunities,
                JoinableCommunities = new SelectList(allCommunities.ExceptBy(myCommunities.Select(c => c.Id), c => c.Id)
                                                                   .ExceptBy(appliedCommunities.Select(c => c.Id), c => c.Id)
                                                                   .ExceptBy(administeredCommunities.Select(c => c.Id), c => c.Id), "Id", "Name"),
                JoinedCommunities = [.. myCommunities.ExceptBy(administeredCommunities.Select(c => c.Id), c => c.Id)]
            });
        }
    }
}
