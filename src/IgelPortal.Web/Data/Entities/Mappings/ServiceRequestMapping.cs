using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class ServiceRequestMapping : IEntityTypeConfiguration<ServiceRequest>
    {
        public void Configure(EntityTypeBuilder<ServiceRequest> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(x => x.ServiceId).IsRequired();
            builder.Property(x => x.CommunityId).IsRequired();
            builder.Property(x => x.RequestToken).IsRequired();
            builder.Property(x => x.FulfillmentDate);
            builder.Property(x => x.CreationDate).IsRequired();
            builder.Property(x => x.ChangeDate).IsRequired();
            builder.Property(x => x.DeletedDate);

            builder.HasMany<ServiceRequestResponse>(e => e.Responses).WithOne();
            builder.HasOne<TerritoryService>(e => e.Service).WithMany();
            builder.HasOne<Hedgehog>(e => e.Hedgehog).WithMany(e => e.ServiceRequests);
            builder.HasOne<Community>(e => e.Community).WithMany(e => e.ServiceRequests);
        }
    }
}
