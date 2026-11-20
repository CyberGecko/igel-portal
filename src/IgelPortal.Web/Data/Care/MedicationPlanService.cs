using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IgelPortal.Web.Data.Care;

public sealed record CareTask(Guid HedgehogId, string HedgehogName, MedicationPlan Plan, IReadOnlyList<MedicationDose> Due);

public sealed class MedicationPlanService(ApplicationDbContext db, CareAccess access, CareJournalService journal)
{
    public Task<CareResult> CreateAsync(Guid id, ClaimsPrincipal user, Guid episodeId, string? medication, string? amount,
        string? instructions, IReadOnlyList<DateTime> schedule, Guid planId, Guid? replacePlanId) => journal.ChangeAsync(id, user, async (hedgehog, permissions) =>
    {
        if (!permissions.CanPlan || hedgehog.CommunityId is not Guid groupId) return CareResult.Denied;
        medication = medication?.Trim();
        amount = amount?.Trim();
        instructions = instructions?.Trim();
        if (string.IsNullOrEmpty(medication) || medication.Length > 160 || string.IsNullOrEmpty(amount) || amount.Length > 80 ||
            instructions?.Length > 1000 || schedule.Count is < 1 or > 360 || planId == Guid.Empty) return CareResult.Invalid;

        var episode = await db.CareEpisodes.SingleOrDefaultAsync(item => item.Id == episodeId && item.OpenHedgehogId == id);
        if (episode is null || schedule.Any(time => time < episode.StartedAtUtc)) return CareResult.Conflict;
        var existingId = await db.MedicationPlans.Where(item => item.Id == planId)
            .Select(item => (Guid?)item.EpisodeId).SingleOrDefaultAsync();
        if (existingId.HasValue)
            return existingId.Value == episode.Id ? CareResult.Saved : CareResult.Conflict;

        var now = DateTime.UtcNow;

        if (replacePlanId is Guid previousId)
        {
            var previous = await db.MedicationPlans.Include(plan => plan.Doses.Where(dose => dose.Status == DoseStatus.Pending))
                .SingleOrDefaultAsync(plan => plan.Id == previousId && plan.EpisodeId == episodeId && plan.CommunityId == groupId && plan.EndedAtUtc == null);
            if (previous is null) return CareResult.Conflict;
            Stop(previous, now);
        }

        db.MedicationPlans.Add(new MedicationPlan
        {
            Id = planId, EpisodeId = episode.Id, CommunityId = groupId, Medication = medication, Amount = amount, Instructions = instructions,
            CreatedById = permissions.UserId, CreatedBy = permissions.Recorder, CreatedAtUtc = now,
            Doses = schedule.Select(time => new MedicationDose { ScheduledAtUtc = time }).ToList()
        });
        return CareResult.Saved;
    });

    public Task<CareResult> OfferAsync(Guid id, ClaimsPrincipal user, Guid planId, string? helperId) => journal.ChangeAsync(id, user, async (hedgehog, permissions) =>
    {
        if (!permissions.CanPlan) return CareResult.Denied;
        var plan = await ActivePlans(id, hedgehog.CommunityId).SingleOrDefaultAsync(item => item.Id == planId);
        if (plan is null) return CareResult.Conflict;
        if (!string.IsNullOrEmpty(helperId) && !(await access.EligibleHelpersAsync(hedgehog)).Any(item => item.Id == helperId))
            return CareResult.Denied;

        if (plan.AssignedUserId != helperId || plan.Assignment == PlanAssignment.Declined)
        {
            plan.AssignedUserId = string.IsNullOrEmpty(helperId) ? null : helperId;
            plan.Assignment = plan.AssignedUserId is null ? PlanAssignment.Unassigned : PlanAssignment.Offered;
            plan.AssignmentChangedAtUtc = DateTime.UtcNow;
        }
        return CareResult.Saved;
    });

    public Task<CareResult> RespondAsync(Guid id, ClaimsPrincipal user, Guid planId, bool accept) =>
        journal.ChangeAsync(id, user, async (hedgehog, permissions) =>
        {
            if (!permissions.CanRecord) return CareResult.Denied;
            var plan = await ActivePlans(id, hedgehog.CommunityId).SingleOrDefaultAsync(item => item.Id == planId);
            if (plan is null || plan.Assignment != PlanAssignment.Offered) return CareResult.Conflict;
            if (plan.AssignedUserId != permissions.UserId ||
                !(await access.EligibleHelpersAsync(hedgehog)).Any(item => item.Id == permissions.UserId)) return CareResult.Denied;
            plan.Assignment = accept ? PlanAssignment.Accepted : PlanAssignment.Declined;
            plan.AssignmentChangedAtUtc = DateTime.UtcNow;
            return CareResult.Saved;
        });

    public Task<CareResult> StopAsync(Guid id, ClaimsPrincipal user, Guid planId) => journal.ChangeAsync(id, user, async (hedgehog, permissions) =>
    {
        if (!permissions.CanPlan) return CareResult.Denied;
        var plan = await ActivePlans(id, hedgehog.CommunityId).Include(item => item.Doses.Where(dose => dose.Status == DoseStatus.Pending))
            .SingleOrDefaultAsync(item => item.Id == planId);
        if (plan is null) return CareResult.Conflict;
        Stop(plan, DateTime.UtcNow);
        return CareResult.Saved;
    });

    public Task<CareResult> RecordAsync(Guid id, ClaimsPrincipal user, Guid planId, Guid doseId, bool given, DateTime? givenAt,
        string? amount, string? note) => journal.ChangeAsync(id, user, async (hedgehog, permissions) =>
    {
        if (!permissions.CanRecord) return CareResult.Denied;
        var plan = await ActivePlans(id, hedgehog.CommunityId).SingleOrDefaultAsync(item => item.Id == planId);
        if (plan is null) return CareResult.Conflict;
        if (plan.Assignment != PlanAssignment.Accepted || plan.AssignedUserId != permissions.UserId ||
            !(await access.EligibleHelpersAsync(hedgehog)).Any(item => item.Id == permissions.UserId)) return CareResult.Denied;

        var dose = await db.MedicationDoses.SingleOrDefaultAsync(item => item.Id == doseId && item.PlanId == planId);
        if (dose is null || dose.Status != DoseStatus.Pending) return CareResult.Conflict;
        var episode = await db.CareEpisodes.SingleAsync(item => item.Id == plan.EpisodeId);
        var now = DateTime.UtcNow;
        amount = amount?.Trim();
        note = note?.Trim();
        if (note?.Length > 1000 ||
            (given ? (!givenAt.HasValue || givenAt < episode.StartedAtUtc || givenAt > now || string.IsNullOrEmpty(amount) || amount.Length > 80)
                   : string.IsNullOrEmpty(note))) return CareResult.Invalid;

        dose.Status = given ? DoseStatus.Given : DoseStatus.Skipped;
        dose.GivenAtUtc = given ? givenAt : null;
        dose.RecordedAtUtc = now;
        dose.RecordedById = permissions.UserId;
        dose.RecordedBy = permissions.Recorder;
        dose.ActualAmount = given ? amount : null;
        dose.Note = note;
        return CareResult.Saved;
    });

    public async Task<IReadOnlyList<CareTask>> TasksAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return [];
        var candidates = await (from plan in db.MedicationPlans.AsNoTracking()
                                join episode in db.CareEpisodes on plan.EpisodeId equals episode.Id
                                join hedgehog in db.Hedgehogs.AsNoTracking() on episode.HedgehogId equals hedgehog.Id
                                where plan.AssignedUserId == userId && (plan.Assignment == PlanAssignment.Offered || plan.Assignment == PlanAssignment.Accepted)
                                    && plan.EndedAtUtc == null && episode.OpenHedgehogId != null && hedgehog.DeletedDate == null &&
                                    hedgehog.CommunityId == plan.CommunityId
                                select new { Plan = plan, Hedgehog = hedgehog }).ToListAsync();
        var tasks = new List<CareTask>();
        foreach (var candidate in candidates)
        {
            var permissions = await access.ForAsync(candidate.Hedgehog, user);
            if (!permissions.CanRecord || !(await access.EligibleHelpersAsync(candidate.Hedgehog)).Any(item => item.Id == userId))
                continue;

            IReadOnlyList<MedicationDose> due = candidate.Plan.Assignment == PlanAssignment.Offered ? [] :
                await db.MedicationDoses.AsNoTracking().Where(dose => dose.PlanId == candidate.Plan.Id && dose.Status == DoseStatus.Pending)
                    .OrderBy(dose => dose.ScheduledAtUtc).Take(4).ToListAsync();
            if (candidate.Plan.Assignment == PlanAssignment.Offered || due.Count > 0)
                tasks.Add(new(candidate.Hedgehog.Id, candidate.Hedgehog.Name, candidate.Plan, due));
        }
        return tasks.OrderBy(item => item.Plan.Assignment != PlanAssignment.Offered)
            .ThenBy(item => item.Due.FirstOrDefault()?.ScheduledAtUtc).ToList();
    }

    private IQueryable<MedicationPlan> ActivePlans(Guid id, Guid? groupId) =>
        db.MedicationPlans.Where(plan => plan.CommunityId == groupId && plan.EndedAtUtc == null &&
            db.Communities.Any(group => group.Id == groupId && group.DeletedDate == null) &&
            db.CareEpisodes.Any(episode => episode.Id == plan.EpisodeId && episode.OpenHedgehogId == id));

    internal static void Stop(MedicationPlan plan, DateTime now)
    {
        plan.EndedAtUtc = now;
        foreach (var dose in plan.Doses.Where(item => item.Status == DoseStatus.Pending))
            dose.Status = DoseStatus.Cancelled;
    }
}
