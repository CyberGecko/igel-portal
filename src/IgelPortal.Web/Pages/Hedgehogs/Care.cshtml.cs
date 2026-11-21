using IgelPortal.Web.Data.Care;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace IgelPortal.Web.Pages.Hedgehogs;

[Authorize(Roles = RoleNames.USER)]
public sealed class CareModel(CareJournalService journal, MedicationPlanService medications, CareClock clock,
    IStringLocalizer<SharedPageResource> text) : PageModel
{
    public sealed record PlanInput(Guid PlanId, string? Medication, string? Amount, string? Instructions,
        string? StartDate, string? EndDate, string?[] DailyTimes, Guid? ReplacePlanId);
    public sealed record DoseInput(Guid PlanId, string? GivenAt, string? Amount, string? Note);

    public PlanInput? PlanDraft { get; private set; }
    public DoseInput? DoseDraft { get; private set; }
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
        var scheduleIsValid = MedicationSchedule.TryBuild(clock, DateOnly.FromDateTime(clock.LocalNow), startDate,
            endDate, dailyTimes, out var schedule, out var invalidField);
        if (!scheduleIsValid)
        {
            ModelState.AddModelError(invalidField!, text[invalidField switch
            {
                "startDate" => "Choose a start date of today or later.",
                "endDate" => "Choose an end date within 90 days, on or after the start date.",
                _ => "Enter up to four distinct times that exist and are unambiguous in the portal time zone."
            }]);
        }
        if (string.IsNullOrWhiteSpace(medication) || medication.Trim().Length > 160)
            ModelState.AddModelError("medication", text["Enter a medication name of up to 160 characters."]);
        if (string.IsNullOrWhiteSpace(amount) || amount.Trim().Length > 80)
            ModelState.AddModelError("amount", text["Enter the prescribed amount and unit, up to 80 characters."]);
        if (instructions?.Trim().Length > 1000)
            ModelState.AddModelError("instructions", text["Use no more than 1000 characters."]);

        var result = Valid
            ? await medications.CreateAsync(id, User, episodeId, medication, amount, instructions, schedule, planId, replacePlanId)
            : CareResult.Invalid;
        if (result is not (CareResult.Invalid or CareResult.Conflict)) return Done(id, result, "medication");

        if (result == CareResult.Conflict)
            ModelState.AddModelError("", text["Check the start of the stay and the plan you want to replace."]);
        else if (Valid)
            ModelState.AddModelError("", text["Check the entered values and try again."]);
        PlanDraft = new(planId, medication, amount, instructions, startDate, endDate, dailyTimes ?? [], replacePlanId);
        return await OnGetAsync(id, episodeId, null);
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
        if (doseId == Guid.Empty) ModelState.AddModelError("doseId", text["Select a scheduled dose."]);
        if (!given.HasValue) ModelState.AddModelError("", text["Choose whether the dose was given."]);
        if (given == true)
        {
            if (!when.HasValue || when > DateTime.UtcNow)
                ModelState.AddModelError("givenAt", text["Enter a valid administration time that is not in the future."]);
            if (string.IsNullOrWhiteSpace(actualAmount) || actualAmount.Trim().Length > 80)
                ModelState.AddModelError("actualAmount", text["Enter the amount actually given and unit, up to 80 characters."]);
        }
        else if (given == false && string.IsNullOrWhiteSpace(note))
        {
            ModelState.AddModelError("note", text["Enter a reason for skipping the dose."]);
        }
        if (note?.Trim().Length > 1000)
            ModelState.AddModelError("note", text["Use no more than 1000 characters."]);

        var result = Valid && given.HasValue
            ? await medications.RecordAsync(id, User, planId, doseId, given.Value, when, actualAmount, note)
            : CareResult.Invalid;
        if (result != CareResult.Invalid)
        {
            return Done(id, result, "medication");
        }

        if (Valid) ModelState.AddModelError("givenAt", text["The time must be within the current stay."]);
        DoseDraft = new(planId, givenAt, actualAmount, note);
        return await OnGetAsync(id, null, doseId == Guid.Empty ? null : doseId);
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
