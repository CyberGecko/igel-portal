using IgelPortal.Web.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class HedgehogMapping : IEntityTypeConfiguration<Hedgehog>
    {
        public void Configure(EntityTypeBuilder<Hedgehog> builder)
        {
            builder.AddBaseMappings<Hedgehog>();

            builder.Property(e => e.AttachedUser);
            builder.Property(e => e.ImageString);
            builder.Property(e => e.BiologicalSex);

            builder.HasOne<Territory>(e => e.HomeTerritory).WithMany(e => e.HomeHedgehogs);
            builder.HasOne<Territory>(e => e.CurrentTerritory).WithMany(e => e.CurrentHedgehogs);
            builder.HasOne<Community>(e => e.Community).WithMany(e => e.Hedgehogs);
            builder.HasOne<TerritoryAttachedService>(e => e.UsedService).WithMany(e => e.Hedgehogs);
        }
    }
}
