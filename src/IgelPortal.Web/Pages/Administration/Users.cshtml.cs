using IgelPortal.Web.Data;
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
    public class UsersModel(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager = userManager;

        public IList<UserAdministration> Users { get; set; } = default!;

        public IList<String> AvailableRoles { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var cancellationToken = HttpContext.RequestAborted;
            var userDataList = await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);
            var roles = await dbContext.Roles.AsNoTracking()
                .Where(role => role.Name != null && role.Name != "")
                .Select(role => new { role.Id, Name = role.Name! }).ToListAsync(cancellationToken);
            var assignments = await dbContext.UserRoles.AsNoTracking().ToListAsync(cancellationToken);
            var roleNames = roles.ToDictionary(role => role.Id, role => role.Name);
            var rolesByUser = assignments.Where(assignment => roleNames.ContainsKey(assignment.RoleId))
                .ToLookup(assignment => assignment.UserId, assignment => roleNames[assignment.RoleId]);

            this.Users = userDataList.Select(c => new UserAdministration()
            {
                Id = c.Id,
                UserName = c.UserName,
                Email = c.Email,
                AccessFailedCount = c.AccessFailedCount,
                EmailConfirmed = c.EmailConfirmed,
                TwoFactorEnabled = c.TwoFactorEnabled,
                Roles = rolesByUser[c.Id].ToList()
            }).ToList();

            this.AvailableRoles = roles.Select(role => role.Name).ToList();
        }

        public async Task<IActionResult> OnPostRemoveRole(String userId, String roleName)
        {
            var user = await this.userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    await userManager.UpdateSecurityStampAsync(user);
                }
            }

            return base.RedirectToPage();
        }
        public async Task<IActionResult> OnPostAddRole(String userId, String roleName)
        {
            var user = await this.userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    await userManager.UpdateSecurityStampAsync(user);
                }
            }

            return base.RedirectToPage();
        }
    }
}
