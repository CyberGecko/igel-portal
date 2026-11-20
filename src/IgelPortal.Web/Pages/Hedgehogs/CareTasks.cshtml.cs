using IgelPortal.Web.Data;
using IgelPortal.Web.Data.Care;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IgelPortal.Web.Pages.Hedgehogs;

[Authorize(Roles = RoleNames.USER)]
public sealed class CareTasksModel(ApplicationDbContext db, CareAccess access, MedicationPlanService medication, CareClock clock) : PageModel
{
    public IReadOnlyList<CareTask> Tasks { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Hedgehogs { get; private set; } = [];
    public CareClock Clock => clock;

    public async Task OnGetAsync()
    {
        Tasks = await medication.TasksAsync(User);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.Identity?.Name;
        var candidates = await db.Hedgehogs.AsNoTracking().Where(item => item.DeletedDate == null &&
            (item.AttachedUser == userName || db.CareHelpers.Any(helper => helper.HedgehogId == item.Id && helper.UserId == userId &&
                helper.CommunityId == item.CommunityId) ||
             db.CommunityMembers.Any(member => member.CommunityId == item.CommunityId && member.Community.DeletedDate == null &&
                 member.UserName == userName && (member.CommunityRoleName == CommunityRoles.OWNER ||
                 member.CommunityRoleName == CommunityRoles.MEDICATION_PLANNER)))).OrderBy(item => item.Name).ToListAsync();
        var hedgehogs = new List<(Guid, string)>();
        foreach (var item in candidates)
        {
            if ((await access.ForAsync(item, User)).CanRead) hedgehogs.Add((item.Id, item.Name));
        }
        Hedgehogs = hedgehogs;
    }
}
