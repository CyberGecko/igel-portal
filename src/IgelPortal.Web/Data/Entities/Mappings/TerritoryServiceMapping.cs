using IgelPortal.Web.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class TerritoryServiceMapping : IEntityTypeConfiguration<TerritoryService>
    {
        public void Configure(EntityTypeBuilder<TerritoryService> builder)
        {
            builder.AddBaseMappings<TerritoryService>();
        }
    }
}
