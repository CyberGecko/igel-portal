using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Data.Care;

public enum CareEntryKind { Weight, Observation, Finding, Mark, Update }
public enum BodyView { Top, Left, Right }
public enum BodyRegion { Head, BackLeft, BackRight, FrontLegLeft, FrontLegRight, HindLegLeft, HindLegRight, Belly, Other }
public enum CareEndReason { Released, Transferred, Died, Other }
public enum PlanAssignment { Unassigned, Offered, Accepted, Declined }
public enum DoseStatus { Pending, Given, Skipped, Cancelled }

public sealed class CareEpisode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HedgehogId { get; set; }
    public Guid? OpenHedgehogId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public CareEndReason? EndReason { get; set; }
}

public sealed class CareHelper
{
    public Guid HedgehogId { get; set; }
    public Guid CommunityId { get; set; }
    [MaxLength(255)] public required string UserId { get; set; }
}

public sealed class CareEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EpisodeId { get; set; }
    public Guid? ParentEntryId { get; set; }
    public CareEntryKind Kind { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime EnteredAtUtc { get; set; }
    [MaxLength(255)] public required string EnteredById { get; set; }
    [MaxLength(256)] public required string EnteredBy { get; set; }
    [MaxLength(2000)] public string? Note { get; set; }
    public int? WeightGrams { get; set; }
    public BodyView? View { get; set; }
    public BodyRegion? Region { get; set; }
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public sealed class MedicationPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EpisodeId { get; set; }
    public Guid CommunityId { get; set; }
    [MaxLength(160)] public required string Medication { get; set; }
    [MaxLength(80)] public required string Amount { get; set; }
    [MaxLength(1000)] public string? Instructions { get; set; }
    [MaxLength(255)] public required string CreatedById { get; set; }
    [MaxLength(256)] public required string CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    [MaxLength(255)] public string? AssignedUserId { get; set; }
    public PlanAssignment Assignment { get; set; }
    public DateTime? AssignmentChangedAtUtc { get; set; }
    public ICollection<MedicationDose> Doses { get; set; } = [];
}

public sealed class MedicationDose
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public DoseStatus Status { get; set; }
    public DateTime? GivenAtUtc { get; set; }
    public DateTime? RecordedAtUtc { get; set; }
    [MaxLength(255)] public string? RecordedById { get; set; }
    [MaxLength(256)] public string? RecordedBy { get; set; }
    [MaxLength(80)] public string? ActualAmount { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
}
