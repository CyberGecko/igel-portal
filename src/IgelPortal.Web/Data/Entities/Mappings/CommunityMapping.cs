using IgelPortal.Web.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class CommunityMapping : IEntityTypeConfiguration<Community>
    {
        public void Configure(EntityTypeBuilder<Community> builder)
        {
            builder.AddBaseMappings<Community>();

            builder.Property(e => e.UserName).IsRequired();
            builder.Property(e => e.Description);
            builder.Property(e => e.ContactName).IsRequired();
            builder.Property(e => e.ContactMail).IsRequired();
            builder.Property(e => e.ContactPhone);

            builder.HasMany<Territory>(e => e.Territories).WithOne(e => e.Community);
            builder.HasMany<Hedgehog>(e => e.Hedgehogs).WithOne(e => e.Community);
        }
    }
}
