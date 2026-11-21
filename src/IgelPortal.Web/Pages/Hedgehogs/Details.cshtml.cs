using IgelPortal.Web.Data.Care;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Hedgehogs
{
    [Authorize(Roles = RoleNames.USER)]
    public class DetailsModel(IHedgehogDataService dataService, IAuthorizationService authorization, CareAccess careAccess) : PageModel
    {
        private readonly IHedgehogDataService dataService = dataService;

        public bool CanReadCare { get; private set; }
        public bool CanRequestPlace { get; private set; }

        public Hedgehog Hedgehog { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id, String? filter)
        {
            base.ViewData["Filter"] = filter;
            if (!id.HasValue)
            {
                return base.NotFound();
            }

            var hedgehog = await this.dataService.FindOne(id.Value);
            if (hedgehog is null || hedgehog.IsDeleted)
            {
                return base.NotFound();
            }
            CanRequestPlace = hedgehog.CommunityId is Guid groupId &&
                (await authorization.AuthorizeAsync(User, groupId, PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)).Succeeded;
            if (hedgehog.AttachedUser != User.Identity?.Name && !User.IsInRole(RoleNames.ADMIN) && !CanRequestPlace)
            {
                return Forbid();
            }
            this.Hedgehog = hedgehog;
            CanReadCare = (await careAccess.ForAsync(hedgehog.Id, hedgehog.CommunityId, hedgehog.AttachedUser, User)).CanRead;
            return base.Page();
        }
    }
}
