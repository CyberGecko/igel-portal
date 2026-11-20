using IgelPortal.Web.Data.Care;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgelPortal.Web.Tests;

[TestClass]
public class CareRulesTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly CareClock clock = new(new ConfigurationBuilder().Build());

    [TestMethod]
    [DataRow("2026-01-01T09:00", 8)]
    [DataRow("2026-07-01T09:00", 7)]
    public void CareTimesAreConvertedToUtcUsingThePortalTimeZone(string value, int utcHour)
    {
        Assert.IsTrue(clock.TryParseLocal(value, out var utc));
        Assert.AreEqual(DateTimeKind.Utc, utc.Kind);
        Assert.AreEqual(utcHour, utc.Hour);
    }

    [TestMethod]
    [DataRow("2026-03-29T02:30")]
    [DataRow("2026-10-25T02:30")]
    [DataRow("29.03.2026 03:30")]
    public void CareTimesRejectAmbiguousNonexistentOrMalformedInput(string value)
    {
        Assert.IsFalse(clock.TryParseLocal(value, out _));
    }

    [TestMethod]
    public void MedicationScheduleOrdersDosesAndIncludesTheLastDay()
    {
        var valid = MedicationSchedule.TryBuild(clock, Today, "2026-01-01", "2026-01-02",
            ["20:00", null, "08:00"], out var doses);

        Assert.IsTrue(valid);
        CollectionAssert.AreEqual(new[] { Now.AddHours(-5), Now.AddHours(7), Now.AddHours(19), Now.AddHours(31) },
            doses.ToArray());
    }

    [TestMethod]
    public void MedicationScheduleAllowsNinetyDaysWithFourDosesPerDay()
    {
        Assert.IsTrue(MedicationSchedule.TryBuild(clock, Today, "2026-01-01", "2026-03-31",
            ["06:00", "10:00", "14:00", "18:00"], out var doses));
        Assert.HasCount(360, doses);
    }

    [TestMethod]
    [DataRow("2025-12-31", "2026-01-02", "08:00")]
    [DataRow("2026-01-02", "2026-01-01", "08:00")]
    [DataRow("2026-01-01", "2026-04-01", "08:00")]
    [DataRow("2026-01-01", "2026-01-01", "")]
    [DataRow("2026-01-01", "2026-01-01", "24:00")]
    [DataRow("2026-01-01", "2026-01-01", "08:00,08:00")]
    [DataRow("2026-01-01", "2026-01-01", "08:00,09:00,10:00,11:00,12:00")]
    [DataRow("2026-03-28", "2026-03-29", "02:30")]
    [DataRow("2026-10-24", "2026-10-25", "02:30")]
    public void MedicationScheduleRejectsInvalidInputsWithoutReturningPartialDoses(string start, string end, string times)
    {
        Assert.IsFalse(MedicationSchedule.TryBuild(clock, Today, start, end, times.Split(','), out var doses));
        Assert.IsEmpty(doses);
    }

    [TestMethod]
    [DataRow(null, false)]
    [DataRow(0, false)]
    [DataRow(1, true)]
    [DataRow(10000, true)]
    [DataRow(10001, false)]
    public void WeightEntriesRequireGramsWithinTheAllowedRange(int? grams, bool valid)
    {
        Assert.AreEqual(valid, CareJournalService.IsValidEntry(CareEntryKind.Weight, Now, Now, grams, null, null, null, null, null));
    }

    [TestMethod]
    [DataRow(CareEntryKind.Mark, null, null, null, null, false)]
    [DataRow(CareEntryKind.Mark, null, BodyRegion.Head, null, null, true)]
    [DataRow(CareEntryKind.Mark, null, null, 0, 1000, false)]
    [DataRow(CareEntryKind.Finding, BodyView.Top, null, 0, 1000, true)]
    [DataRow(CareEntryKind.Finding, BodyView.Top, null, null, 1000, false)]
    [DataRow(CareEntryKind.Finding, BodyView.Top, null, 1001, 0, false)]
    [DataRow(CareEntryKind.Observation, null, BodyRegion.Head, null, null, false)]
    public void BodyMapValidatesRegionsAndPairedCoordinates(CareEntryKind kind, BodyView? view, BodyRegion? region, int? x, int? y, bool valid)
    {
        Assert.AreEqual(valid, CareJournalService.IsValidEntry(kind, Now, Now, null, "Aufzeichnung", view, region, x, y));
    }

    [TestMethod]
    public void JournalRejectsFutureEntries()
    {
        Assert.IsFalse(CareJournalService.IsValidEntry(CareEntryKind.Observation, Now.AddSeconds(1), Now, null, "Beobachtung",
            null, null, null, null));
    }

    [TestMethod]
    public void EndingMedicationPlanCancelsPendingDosesAndPreservesRecordedOutcomes()
    {
        var plan = new MedicationPlan
        {
            Medication = "Medication", Amount = "Dose", CreatedById = "owner", CreatedBy = "Owner",
            Doses =
            [
                new() { Status = DoseStatus.Pending },
                new() { Status = DoseStatus.Given, GivenAtUtc = Now.AddHours(-1) },
                new() { Status = DoseStatus.Skipped, Note = "Skipped" },
                new() { Status = DoseStatus.Cancelled }
            ]
        };

        MedicationPlanService.Stop(plan, Now);

        Assert.AreEqual(Now, plan.EndedAtUtc);
        CollectionAssert.AreEqual(new[] { DoseStatus.Cancelled, DoseStatus.Given, DoseStatus.Skipped, DoseStatus.Cancelled },
            plan.Doses.Select(dose => dose.Status).ToArray());
        Assert.AreEqual(Now.AddHours(-1), plan.Doses.ElementAt(1).GivenAtUtc);
        Assert.AreEqual("Skipped", plan.Doses.ElementAt(2).Note);
    }
}
