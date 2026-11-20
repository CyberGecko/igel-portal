using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class ServiceRequestResponseMapping : IEntityTypeConfiguration<ServiceRequestResponse>
    {
        public void Configure(EntityTypeBuilder<ServiceRequestResponse> builder)
        {
            builder.HasKey(e => new { e.ServiceRequestId, e.RespondingUserName});

            builder.Property(x => x.ServiceRequestId).IsRequired();
            builder.Property(x => x.RespondingUserName).IsRequired();
            builder.Property(x => x.ResponseDate).IsRequired();
        }
    }
}
