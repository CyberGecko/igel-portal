using IgelPortal.Web.Data.Care;
using IgelPortal.Web.Data.Entities;
using IgelPortal.Web.Data.Entities.Mappings;
using IgelPortal.Web.Data.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IgelPortal.Web.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public virtual DbSet<Hedgehog> Hedgehogs { get; set; }
        public virtual DbSet<Territory> Territories { get; set; }
        public virtual DbSet<Community> Communities { get; set; }
        public virtual DbSet<CommunityMember> CommunityMembers { get; set; }
        public virtual DbSet<TerritoryService> TerritoryServices { get; set; }
        public virtual DbSet<TerritoryAttachedService> TerritoryAttachedServices { get; set; }
        public virtual DbSet<ServiceRequest> ServiceRequests { get; set; }
        public virtual DbSet<ServiceRequestResponse> ServiceRequestResponses { get; set; }

        public DbSet<AuthenticatorTransport> AuthenticatorTransports { get; set; }
        public DbSet<DevicePublicKey> DevicePublicKeys { get; set; }
        public DbSet<PublicKeyCredential> PublicKeyCredentials { get; set; }

        public DbSet<AltchaReplay> AltchaReplays { get; set; }

        public DbSet<CareEpisode> CareEpisodes => Set<CareEpisode>();
        public DbSet<CareHelper> CareHelpers => Set<CareHelper>();
        public DbSet<CareEntry> CareEntries => Set<CareEntry>();
        public DbSet<MedicationPlan> MedicationPlans => Set<MedicationPlan>();
        public DbSet<MedicationDose> MedicationDoses => Set<MedicationDose>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureCare();

            modelBuilder.Entity<AltchaReplay>(replay =>
            {
                replay.HasKey(entry => entry.Id);
                replay.Property(entry => entry.Id).HasMaxLength(64);
                replay.HasIndex(entry => entry.ExpiresAtUtc);
            });

            modelBuilder.ApplyConfiguration<Hedgehog>(new HedgehogMapping());
            modelBuilder.ApplyConfiguration<Territory>(new TerritoryMapping());
            modelBuilder.ApplyConfiguration<Community>(new CommunityMapping());
            modelBuilder.ApplyConfiguration<CommunityMember>(new CommunityMemberMapping());
            modelBuilder.ApplyConfiguration<TerritoryService>(new TerritoryServiceMapping());
            modelBuilder.ApplyConfiguration<TerritoryAttachedService>(new TerritoryAttachedServiceMapping());
            modelBuilder.ApplyConfiguration<ServiceRequest>(new ServiceRequestMapping());
            modelBuilder.ApplyConfiguration<ServiceRequestResponse>(new ServiceRequestResponseMapping());

            modelBuilder.ApplyConfiguration<AuthenticatorTransport>(new AuthenticatorTransportMapping());
            modelBuilder.ApplyConfiguration<DevicePublicKey>(new DevicePublicKeyMapping());
            modelBuilder.ApplyConfiguration<PublicKeyCredential>(new PublicKeyCredentialMapping());
        }
    }
}
