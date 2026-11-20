using IgelPortal.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IgelPortal.Web.Data.Care;

public sealed record CareJournal(Hedgehog Hedgehog, CarePermissions Permissions, IReadOnlyList<CareEpisode> Episodes,
    CareEpisode? Episode, IReadOnlyList<CareEntry> Entries, IReadOnlyList<CareEntry> ActiveMarks,
    IReadOnlyList<MedicationPlan> Plans, IReadOnlyList<CarePerson> Eligible, IReadOnlyList<CarePerson> CommunityUsers,
    IReadOnlySet<string> TeamIds, IReadOnlyDictionary<string, string> AssignedNames, IReadOnlyDictionary<Guid, string> ParentNotes);

public enum CareResult { Saved, Denied, Missing, Invalid, Conflict }

public sealed class CareJournalService(ApplicationDbContext db, CareAccess access)
{
    public async Task<(CareResult Result, CareJournal? Journal)> ReadAsync(Guid id, Guid? episodeId, ClaimsPrincipal user)
    {
        var hedgehog = await db.Hedgehogs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id && item.DeletedDate == null);
        if (hedgehog is null) return (CareResult.Missing, null);
        var permissions = await access.ForAsync(hedgehog, user);
        if (!permissions.CanRead) return (CareResult.Denied, null);

        var episodes = await db.CareEpisodes.AsNoTracking().Where(item => item.HedgehogId == id)
            .OrderByDescending(item => item.OpenHedgehogId.HasValue).ThenByDescending(item => item.StartedAtUtc).ToListAsync();
        var episode = episodeId.HasValue ? episodes.SingleOrDefault(item => item.Id == episodeId) : episodes.FirstOrDefault();
        if (episodeId.HasValue && episode is null) return (CareResult.Missing, null);

        var entries = episode is null ? [] : await db.CareEntries.AsNoTracking().Where(item => item.EpisodeId == episode.Id)
            .OrderByDescending(item => item.OccurredAtUtc).ThenByDescending(item => item.EnteredAtUtc).ToListAsync();
        var marks = await (from entry in db.CareEntries.AsNoTracking()
                           join stay in db.CareEpisodes on entry.EpisodeId equals stay.Id
                           where stay.HedgehogId == id && entry.Kind == CareEntryKind.Mark && entry.ResolvedAtUtc == null
                           orderby entry.OccurredAtUtc descending
                           select entry).ToListAsync();
        var plans = episode is null ? [] : await db.MedicationPlans.AsNoTracking().Where(item => item.EpisodeId == episode.Id)
            .Include(item => item.Doses.OrderBy(dose => dose.ScheduledAtUtc)).OrderBy(item => item.EndedAtUtc.HasValue)
            .ThenByDescending(item => item.CreatedAtUtc).ToListAsync();
        var eligible = permissions.CanPlan || permissions.CanManageTeam ? await access.EligibleHelpersAsync(hedgehog) : [];
        var members = permissions.CanManageTeam ? await access.CommunityUsersAsync(hedgehog) : [];
        var team = await db.CareHelpers.Where(item => item.HedgehogId == id && item.CommunityId == hedgehog.CommunityId)
            .Select(item => item.UserId).ToListAsync();
        var parents = entries.Where(item => item.ParentEntryId.HasValue).Select(item => item.ParentEntryId!.Value).Distinct().ToArray();
        var parentNotes = parents.Length == 0 ? new Dictionary<Guid, string>() :
            await (from entry in db.CareEntries
                   join stay in db.CareEpisodes on entry.EpisodeId equals stay.Id
                   where stay.HedgehogId == id && parents.Contains(entry.Id)
                   select new { entry.Id, Note = entry.Note ?? "" }).ToDictionaryAsync(item => item.Id, item => item.Note);
        var assignedIds = plans.Select(item => item.AssignedUserId).OfType<string>().Distinct().ToArray();
        var assignedNames = await db.Users.AsNoTracking().Where(item => assignedIds.Contains(item.Id)).ToListAsync();
        return (CareResult.Saved, new(hedgehog, permissions, episodes, episode, entries, marks, plans, eligible, members,
            team.ToHashSet(), assignedNames.ToDictionary(item => item.Id, item => item.ToString()), parentNotes));
    }

    // All journal and medication mutations for one hedgehog acquire this row before loading permissions and state.
    internal async Task<CareResult> ChangeAsync(Guid id, ClaimsPrincipal user, Func<Hedgehog, CarePermissions, Task<CareResult>> change)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var rows = await db.Hedgehogs.FromSql($"SELECT * FROM Hedgehogs WHERE Id = {id} FOR UPDATE").ToListAsync();
        var hedgehog = rows.SingleOrDefault(item => item.DeletedDate == null);
        if (hedgehog is null) return CareResult.Missing;
        var permissions = await access.ForAsync(hedgehog, user);
        if (!permissions.CanRead) return CareResult.Denied;
        var result = await change(hedgehog, permissions);
        if (result == CareResult.Saved)
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        return result;
    }

    public Task<CareResult> StartAsync(Guid id, ClaimsPrincipal user, DateTime started) => ChangeAsync(id, user, async (_, permissions) =>
    {
        if (!permissions.CanManageTeam) return CareResult.Denied;
        if (started > DateTime.UtcNow) return CareResult.Invalid;
        if (await db.CareEpisodes.AnyAsync(item => item.OpenHedgehogId == id || (item.HedgehogId == id && item.EndedAtUtc > started)))
            return CareResult.Conflict;
        db.CareEpisodes.Add(new CareEpisode { HedgehogId = id, OpenHedgehogId = id, StartedAtUtc = started });
        return CareResult.Saved;
    });

    public Task<CareResult> EndAsync(Guid id, ClaimsPrincipal user, Guid episodeId, CareEndReason reason) => ChangeAsync(id, user, async (_, permissions) =>
    {
        if (!permissions.CanManageTeam) return CareResult.Denied;
        if (!Enum.IsDefined(reason)) return CareResult.Invalid;
        var episode = await db.CareEpisodes.SingleOrDefaultAsync(item => item.Id == episodeId && item.OpenHedgehogId == id);
        if (episode is null) return CareResult.Conflict;
        var now = DateTime.UtcNow;
        episode.OpenHedgehogId = null;
        episode.EndedAtUtc = now;
        episode.EndReason = reason;
        var activePlans = await db.MedicationPlans.Include(item => item.Doses.Where(dose => dose.Status == DoseStatus.Pending))
            .Where(item => item.EpisodeId == episode.Id && item.EndedAtUtc == null).ToListAsync();
        foreach (var plan in activePlans) MedicationPlanService.Stop(plan, now);
        return CareResult.Saved;
    });

    public Task<CareResult> SetHelperAsync(Guid id, ClaimsPrincipal user, string? userId, bool add) => ChangeAsync(id, user, async (hedgehog, permissions) =>
    {
        if (!permissions.CanManageTeam) return CareResult.Denied;
        if (string.IsNullOrWhiteSpace(userId) || userId.Length > 255 || hedgehog.CommunityId is not Guid groupId)
            return CareResult.Invalid;
        var existing = await db.CareHelpers.FindAsync(id, userId);
        if (add)
        {
            if (!(await access.CommunityUsersAsync(hedgehog)).Any(item => item.Id == userId)) return CareResult.Denied;
            if (existing is null)
                db.CareHelpers.Add(new CareHelper { HedgehogId = id, UserId = userId, CommunityId = groupId });
            else
                existing.CommunityId = groupId;
        }
        else if (existing is not null && existing.CommunityId == groupId)
        {
            db.CareHelpers.Remove(existing);
            var offered = await (from plan in db.MedicationPlans
                                 join episode in db.CareEpisodes on plan.EpisodeId equals episode.Id
                                 where episode.OpenHedgehogId == id && plan.EndedAtUtc == null && plan.AssignedUserId == userId
                                 select plan).ToListAsync();
            foreach (var plan in offered)
            {
                plan.AssignedUserId = null;
                plan.Assignment = PlanAssignment.Unassigned;
                plan.AssignmentChangedAtUtc = DateTime.UtcNow;
            }
        }
        return CareResult.Saved;
    });

    public Task<CareResult> AddEntryAsync(Guid id, ClaimsPrincipal user, Guid episodeId, CareEntryKind kind, DateTime when,
        int? grams, string? note, BodyView? view, BodyRegion? region, int? x, int? y) => ChangeAsync(id, user, async (_, permissions) =>
    {
        if (!permissions.CanRecord) return CareResult.Denied;
        note = note?.Trim();
        if (!IsValidEntry(kind, when, DateTime.UtcNow, grams, note, view, region, x, y)) return CareResult.Invalid;
        var point = x.HasValue || y.HasValue;

        var episode = await db.CareEpisodes.SingleOrDefaultAsync(item => item.Id == episodeId && item.OpenHedgehogId == id);
        if (episode is null || when < episode.StartedAtUtc) return CareResult.Conflict;
        db.CareEntries.Add(NewEntry(episodeId, kind, when, permissions, note, grams, point ? view : null, region, x, y));
        return CareResult.Saved;
    });

    public Task<CareResult> FollowUpAsync(Guid id, ClaimsPrincipal user, Guid episodeId, Guid entryId, string? note, bool resolve) =>
        ChangeAsync(id, user, async (_, permissions) =>
        {
            if (!permissions.CanRecord) return CareResult.Denied;
            note = note?.Trim();
            if (string.IsNullOrEmpty(note) || note.Length > 2000) return CareResult.Invalid;
            if (!await db.CareEpisodes.AnyAsync(item => item.Id == episodeId && item.OpenHedgehogId == id)) return CareResult.Conflict;

            var entry = await (from item in db.CareEntries
                               join episode in db.CareEpisodes on item.EpisodeId equals episode.Id
                               where item.Id == entryId && episode.HedgehogId == id && item.ResolvedAtUtc == null
                                   && (item.Kind == CareEntryKind.Mark || (item.Kind == CareEntryKind.Finding && item.EpisodeId == episodeId))
                               select item).SingleOrDefaultAsync();
            if (entry is null) return CareResult.Missing;
            var now = DateTime.UtcNow;
            if (resolve) entry.ResolvedAtUtc = now;
            var update = NewEntry(episodeId, CareEntryKind.Update, now, permissions, note);
            update.ParentEntryId = entry.Id;
            db.CareEntries.Add(update);
            return CareResult.Saved;
        });

    internal static bool IsValidEntry(CareEntryKind kind, DateTime when, DateTime now, int? grams, string? note,
        BodyView? view, BodyRegion? region, int? x, int? y)
    {
        if (kind is not (CareEntryKind.Weight or CareEntryKind.Observation or CareEntryKind.Finding or CareEntryKind.Mark) || when > now)
            return false;
        if (note?.Length > 2000 || (kind == CareEntryKind.Weight ? grams is not (> 0 and <= 10000) : string.IsNullOrEmpty(note)))
            return false;
        var point = x.HasValue || y.HasValue;
        if ((view.HasValue && !Enum.IsDefined(view.Value)) || (region.HasValue && !Enum.IsDefined(region.Value)) ||
            (point && (!view.HasValue || x is not (>= 0 and <= 1000) || y is not (>= 0 and <= 1000))) ||
            (kind == CareEntryKind.Mark && !point && !region.HasValue) ||
            ((kind is CareEntryKind.Weight or CareEntryKind.Observation) && (point || view.HasValue || region.HasValue)))
            return false;

        return true;
    }

    private static CareEntry NewEntry(Guid episodeId, CareEntryKind kind, DateTime when, CarePermissions user, string? note,
        int? grams = null, BodyView? view = null, BodyRegion? region = null, int? x = null, int? y = null) => new()
    {
        EpisodeId = episodeId, Kind = kind, OccurredAtUtc = when, EnteredAtUtc = DateTime.UtcNow,
        EnteredById = user.UserId, EnteredBy = user.Recorder, Note = note, WeightGrams = grams,
        View = view, Region = region, PositionX = x, PositionY = y
    };
}
