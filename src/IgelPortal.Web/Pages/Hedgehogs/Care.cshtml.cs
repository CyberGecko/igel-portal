using IgelPortal.Web.Data.Care;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace IgelPortal.Web.Pages.Hedgehogs;

[Authorize(Roles = RoleNames.USER)]
public sealed class CareModel(CareJournalService journal, MedicationPlanService medications, CareClock clock) : PageModel
{
    public CareJournal Journal { get; private set; } = default!;
    public CareClock Clock => clock;
    public Guid? SelectedDoseId { get; private set; }
    public string Today => clock.LocalNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public async Task<IActionResult> OnGetAsync(Guid id, Guid? episodeId, Guid? doseId)
    {
        var (result, state) = await journal.ReadAsync(id, episodeId, User);
        if (result == CareResult.Missing) return NotFound();
        if (result == CareResult.Denied) return Forbid();

        Journal = state!;
        SelectedDoseId = doseId;
        return Page();
    }

    public async Task<IActionResult> OnPostStartAsync(Guid id, string? startedAt) => Done(id,
        Valid && clock.TryParseLocal(startedAt, out var when) ? await journal.StartAsync(id, User, when) : CareResult.Invalid);

    public async Task<IActionResult> OnPostEndAsync(Guid id, Guid episodeId, CareEndReason? reason) => Done(id,
        Valid && reason.HasValue ? await journal.EndAsync(id, User, episodeId, reason.Value) : CareResult.Invalid);

    public async Task<IActionResult> OnPostTeamAsync(Guid id, string? helperId, bool add) => Done(id,
        Valid ? await journal.SetHelperAsync(id, User, helperId, add) : CareResult.Invalid, "team");

    public async Task<IActionResult> OnPostEntryAsync(Guid id, Guid episodeId, CareEntryKind? kind, string? occurredAt,
        int? grams, string? note, BodyView? view, BodyRegion? region, int? positionX, int? positionY) => Done(id,
        Valid && kind.HasValue && clock.TryParseLocal(occurredAt, out var when)
            ? await journal.AddEntryAsync(id, User, episodeId, kind.Value, when, grams, note, view, region, positionX, positionY)
            : CareResult.Invalid, kind == CareEntryKind.Weight ? "weight" : "body-map");

    public async Task<IActionResult> OnPostFollowUpAsync(Guid id, Guid episodeId, Guid entryId, string? note, bool resolve) => Done(id,
        Valid ? await journal.FollowUpAsync(id, User, episodeId, entryId, note, resolve) : CareResult.Invalid, "body-map");

    public async Task<IActionResult> OnPostPlanAsync(Guid id, Guid episodeId, Guid planId, string? medication, string? amount, string? instructions,
        string? startDate, string? endDate, string?[]? dailyTimes, Guid? replacePlanId)
    {
        if (!Valid || !MedicationSchedule.TryBuild(clock, DateOnly.FromDateTime(clock.LocalNow), startDate, endDate, dailyTimes, out var schedule))
        {
            TempData["CareNotice"] = "Check the date range and administration times. Use 1–4 distinct times per day and a period of at most 90 days. Times must exist and be unambiguous in the portal time zone.";
            return RedirectToPage(pageName: null, pageHandler: null, routeValues: new { id }, fragment: "medication");
        }

        return Done(id, await medications.CreateAsync(id, User, episodeId, medication, amount, instructions, schedule, planId, replacePlanId), "medication");
    }

    public async Task<IActionResult> OnPostOfferAsync(Guid id, Guid planId, string? helperId) => Done(id,
        Valid ? await medications.OfferAsync(id, User, planId, helperId) : CareResult.Invalid, "medication");

    public async Task<IActionResult> OnPostRespondAsync(Guid id, Guid planId, bool accept) => Done(id,
        Valid ? await medications.RespondAsync(id, User, planId, accept) : CareResult.Invalid, "medication");

    public async Task<IActionResult> OnPostStopPlanAsync(Guid id, Guid planId) => Done(id,
        Valid ? await medications.StopAsync(id, User, planId) : CareResult.Invalid, "medication");

    public async Task<IActionResult> OnPostDoseAsync(Guid id, Guid planId, Guid doseId, bool? given, string? givenAt,
        string? actualAmount, string? note)
    {
        DateTime? when = clock.TryParseLocal(givenAt, out var parsed) ? parsed : null;
        return Done(id, Valid && given.HasValue ? await medications.RecordAsync(id, User, planId, doseId, given.Value, when, actualAmount, note)
            : CareResult.Invalid, "medication");
    }

    private bool Valid => ModelState.IsValid;

    private IActionResult Done(Guid id, CareResult result, string? fragment = null)
    {
        if (result == CareResult.Missing) return NotFound();
        if (result == CareResult.Denied) return Forbid();
        TempData["CareNotice"] = result switch
        {
            CareResult.Saved => "Saved.",
            CareResult.Conflict => "The entry has changed or falls outside the current stay. Check the current status and try again.",
            _ => "Check the entered values and try again."
        };
        return RedirectToPage(pageName: null, pageHandler: null, routeValues: new { id }, fragment: fragment);
    }
}
