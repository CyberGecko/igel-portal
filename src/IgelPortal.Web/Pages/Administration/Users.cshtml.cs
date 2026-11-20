using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IgelPortal.Web.Pages.Administration
{
    [Authorize(Roles = RoleNames.ADMIN)]
    public class UsersModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager = userManager;
        private readonly RoleManager<IdentityRole> roleManager = roleManager;

        public IList<UserAdministration> Users { get; set; } = default!;

        public IList<String> AvailableRoles { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var userDataList = await this.userManager.Users.ToListAsync();

            this.Users = userDataList.Select(c => new UserAdministration()
            {
                Id = c.Id,
                UserName = c.UserName,
                Email = c.Email,
                AccessFailedCount = c.AccessFailedCount,
                EmailConfirmed = c.EmailConfirmed,
                TwoFactorEnabled = c.TwoFactorEnabled,
                Roles = this.userManager.GetRolesAsync(c).Result
            }).ToList();

            this.AvailableRoles = this.roleManager.Roles?.Select(c => c.Name ?? String.Empty)?.Where(c => !String.IsNullOrEmpty(c)).ToList() ?? [];
        }

        public async Task<IActionResult> OnGetRemoveRole(String userId, String roleName)
        {
            var user = await this.userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await userManager.RemoveFromRoleAsync(user, roleName);
            }

            return base.RedirectToPage();
        }
        public async Task<IActionResult> OnGetAddRole(String userId, String roleName)
        {
            var user = await this.userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await userManager.AddToRoleAsync(user, roleName);
            }

            return base.RedirectToPage();
        }
    }
}
