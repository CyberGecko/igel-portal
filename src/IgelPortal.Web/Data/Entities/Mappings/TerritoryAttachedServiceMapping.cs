using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class TerritoryAttachedServiceMapping : IEntityTypeConfiguration<TerritoryAttachedService>
    {
        public void Configure(EntityTypeBuilder<TerritoryAttachedService> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.MaxCapacity);

            builder.HasOne<TerritoryService>(e => e.TerritoryService).WithMany();
            builder.HasMany<Hedgehog>(e => e.Hedgehogs).WithOne(e => e.UsedService);
        }
    }
}
