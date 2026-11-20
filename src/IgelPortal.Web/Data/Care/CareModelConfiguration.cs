using IgelPortal.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IgelPortal.Web.Data.Care;

internal static class CareModelConfiguration
{
    public static void ConfigureCare(this ModelBuilder builder)
    {
        builder.Entity<CareEpisode>(entity =>
        {
            entity.HasOne<Hedgehog>().WithMany().HasForeignKey(item => item.HedgehogId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.HedgehogId, item.StartedAtUtc });
            entity.HasIndex(item => item.OpenHedgehogId).IsUnique();
        });
        builder.Entity<CareHelper>(entity =>
        {
            entity.HasKey(item => new { item.HedgehogId, item.UserId });
            entity.HasIndex(item => item.UserId);
            entity.HasOne<Hedgehog>().WithMany().HasForeignKey(item => item.HedgehogId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Identity.ApplicationUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<CareEntry>(entity =>
        {
            entity.HasOne<CareEpisode>().WithMany().HasForeignKey(item => item.EpisodeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CareEntry>().WithMany().HasForeignKey(item => item.ParentEntryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.EpisodeId, item.OccurredAtUtc });
        });
        builder.Entity<MedicationPlan>(entity =>
        {
            entity.HasOne<CareEpisode>().WithMany().HasForeignKey(item => item.EpisodeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(item => item.Doses).WithOne().HasForeignKey(item => item.PlanId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => item.AssignedUserId);
        });
        builder.Entity<MedicationDose>().HasIndex(item => new { item.PlanId, item.ScheduledAtUtc }).IsUnique();
    }
}
