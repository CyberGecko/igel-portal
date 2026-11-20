using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IgelPortal.Web.Pages.Administration
{
    [Authorize(Roles = RoleNames.ADMIN)]
    public class RolesModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager = userManager;
        private readonly RoleManager<IdentityRole> roleManager = roleManager;

        public IList<RoleAdministration> Roles { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var roleDataList = await this.roleManager.Roles.ToListAsync();


            this.Roles = roleDataList.Where(c => !String.IsNullOrWhiteSpace(c.Name)).Select(c => new RoleAdministration()
            {
                Id = c.Id,
                Name = c.Name,
                UsersCount = this.userManager.GetUsersInRoleAsync(c.Name ?? String.Empty).Result.Count
            }).ToList();
        }
    }
}
