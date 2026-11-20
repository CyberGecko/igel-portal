using IgelPortal.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IgelPortal.Web.Data.Care;

public sealed record CarePermissions(string UserId, string UserName, bool CanRead, bool CanRecord, bool CanManageTeam, bool CanPlan)
{
    public string Recorder { get; init; } = UserName;
}

public sealed record CarePerson(string Id, string Name);

public sealed class CareAccess(ApplicationDbContext db)
{
    public Task<CarePermissions> ForAsync(Hedgehog hedgehog, ClaimsPrincipal principal) =>
        hedgehog.DeletedDate.HasValue
            ? Task.FromResult(new CarePermissions("", "", false, false, false, false))
            : ForAsync(hedgehog.Id, hedgehog.CommunityId, hedgehog.AttachedUser, principal);

    public async Task<CarePermissions> ForAsync(Guid hedgehogId, Guid? communityId, string? keeperName, ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var name = principal.Identity?.Name ?? "";
        if (id.Length == 0 || name.Length == 0 || !principal.IsInRole(RoleNames.USER))
        {
            return new(id, name, false, false, false, false);
        }

        var roles = communityId is Guid groupId
            ? await db.CommunityMembers.Where(member => member.CommunityId == groupId && member.Community.DeletedDate == null && member.UserName == name)
                .Select(member => member.CommunityRoleName).ToListAsync()
            : [];
        var owner = roles.Contains(CommunityRoles.OWNER);
        var member = owner || roles.Contains(CommunityRoles.MEMBER) || roles.Contains(CommunityRoles.MODERATOR);
        var keeper = keeperName == name;
        var canPlan = owner || (member && roles.Contains(CommunityRoles.MEDICATION_PLANNER));
        var assigned = member &&
            await db.CareHelpers.AnyAsync(helper => helper.HedgehogId == hedgehogId && helper.CommunityId == communityId && helper.UserId == id);
        var canRecord = keeper || owner || assigned;
        return new(id, name, canRecord || canPlan, canRecord, keeper || owner, canPlan)
        {
            Recorder = (await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id))?.ToString() ?? name
        };
    }

    public async Task<IReadOnlyList<CarePerson>> EligibleHelpersAsync(Hedgehog hedgehog)
    {
        var team = db.CareHelpers.Where(helper => helper.HedgehogId == hedgehog.Id && helper.CommunityId == hedgehog.CommunityId);
        var users = await db.Users.AsNoTracking()
            .Where(user => (user.UserName == hedgehog.AttachedUser || team.Any(helper => helper.UserId == user.Id))
                && (user.UserName == hedgehog.AttachedUser || hedgehog.CommunityId == null ||
                    db.CommunityMembers.Any(member => member.CommunityId == hedgehog.CommunityId && member.Community.DeletedDate == null
                        && member.UserName == user.UserName && (member.CommunityRoleName == CommunityRoles.MEMBER ||
                        member.CommunityRoleName == CommunityRoles.OWNER || member.CommunityRoleName == CommunityRoles.MODERATOR))))
            .ToListAsync();
        return users.Select(user => new CarePerson(user.Id, user.ToString())).OrderBy(person => person.Name).ToList();
    }

    public async Task<IReadOnlyList<CarePerson>> CommunityUsersAsync(Hedgehog hedgehog)
    {
        if (hedgehog.CommunityId is not Guid communityId)
        {
            return [];
        }

        var users = await db.Users.AsNoTracking()
            .Where(user => user.UserName != hedgehog.AttachedUser &&
                db.CommunityMembers.Any(member => member.CommunityId == communityId && member.Community.DeletedDate == null
                    && member.UserName == user.UserName && (member.CommunityRoleName == CommunityRoles.MEMBER ||
                    member.CommunityRoleName == CommunityRoles.MODERATOR || member.CommunityRoleName == CommunityRoles.OWNER)))
            .ToListAsync();
        return users.Select(user => new CarePerson(user.Id, user.ToString())).OrderBy(person => person.Name).ToList();
    }
}
