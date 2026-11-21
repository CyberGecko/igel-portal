using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IgelPortal.Web.Pages.Communities
{
    [Authorize(Roles = $"{RoleNames.ADMIN},{RoleNames.USER}")]
    public class CreateModel(ICommunityDataService dataService, UserManager<ApplicationUser> users) : PageModel
    {
        private readonly ICommunityDataService dataService = dataService;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await users.GetUserAsync(User);
            Community = new Community
            {
                ContactName = user?.ToString(),
                ContactMail = user?.Email ?? string.Empty
            };

            return Page();
        }

        [BindProperty]
        public Community Community { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!base.ModelState.IsValid)
            {
                return base.Page();
            }

            if (String.IsNullOrWhiteSpace(base.User?.Identity?.Name))
            {
                return base.RedirectToPage("/Index");
            }

            this.Community.UserName = base.User.Identity.Name;


            Guid communityId = await this.dataService.Insert(this.Community);

            await this.dataService.AddUserAsOwner(communityId, base.User.Identity);
            await this.dataService.AddUserAsMember(communityId, base.User.Identity);

            return RedirectToPage("./Details", new { id = communityId });
        }
    }
}
