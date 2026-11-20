using IgelPortal.Web.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class TerritoryMapping : IEntityTypeConfiguration<Territory>
    {
        public void Configure(EntityTypeBuilder<Territory> builder)
        {
            builder.AddBaseMappings<Territory>();

            builder.Property(x => x.Street).IsRequired();
            builder.Property(x => x.StreetNumber).IsRequired();
            builder.Property(x => x.PostalCode).IsRequired();
            builder.Property(x => x.City).IsRequired();
            builder.Property(x => x.UserName).IsRequired();
            builder.Property(x => x.Latitude).IsRequired();
            builder.Property(x => x.Longitude).IsRequired();

            builder.HasMany<Hedgehog>(e => e.HomeHedgehogs).WithOne(e => e.HomeTerritory);
            builder.HasMany<Hedgehog>(e => e.CurrentHedgehogs).WithOne(e => e.CurrentTerritory);
            builder.HasOne<Community>(e => e.Community).WithMany(e => e.Territories);
            builder.HasMany<TerritoryAttachedService>(e => e.AttachedServices).WithOne(e => e.Territory);
        }
    }
}
