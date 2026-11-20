using IgelPortal.Web.Data;
using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IgelPortal.Web.Pages.Administration
{
    [Authorize(Roles = RoleNames.ADMIN)]
    public class RolesModel(ApplicationDbContext dbContext) : PageModel
    {

        public IList<RoleAdministration> Roles { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Roles = await dbContext.Roles.AsNoTracking()
                .Where(role => role.Name != null && role.Name != "")
                .Select(role => new RoleAdministration
                {
                    Id = role.Id,
                    Name = role.Name,
                    UsersCount = dbContext.UserRoles.Count(assignment => assignment.RoleId == role.Id)
                }).ToListAsync(HttpContext.RequestAborted);
        }
    }
}
