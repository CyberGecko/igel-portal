using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using IgelPortal.Web.Pages.Hedgehogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Net.Mail;

namespace IgelPortal.Web.Pages.Communities
{
    [Authorize(Roles = RoleNames.USER)]
    public class JoinModel(ICommunityDataService dataService, StringLocalizer<JoinModel> localizer, IEmailSender emailSender, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ICommunityDataService dataService = dataService;
        private readonly StringLocalizer<JoinModel> localizer = localizer;
        private readonly IEmailSender emailSender = emailSender;
        private readonly UserManager<ApplicationUser> userManager = userManager;

        public async Task<IActionResult> OnPost(Guid? communityId)
        {
            if (base.User?.Identity?.Name == null)
            {
                return RedirectToPage("/");
            }

            await dataService.AddUserAsApplicant(communityId, base.User.Identity);
            ApplicationUser? user = await userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return RedirectToPage("/");
            }

            Community? community = await dataService.FindOne(communityId);
            if (String.IsNullOrWhiteSpace(community?.ContactMail))
            {
                return RedirectToPage("/");
            }

            String communityMembersUrl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.RouteValues["page"]}/Members/{communityId}";
            String subject = this.localizer.GetString("A user requested to join your community {0}", community.Name);
            String body = this.localizer.GetString("Member join request body", user, community.Name, communityMembersUrl);
            await emailSender.SendEmailAsync(community.ContactMail, subject, body);

            return RedirectToPage("/");
        }
    }
}
