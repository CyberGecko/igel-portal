using IgelPortal.Web.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class PublicKeyCredentialMapping : IEntityTypeConfiguration<PublicKeyCredential>
    {
        public void Configure(EntityTypeBuilder<PublicKeyCredential> builder)
        {
            builder.ToTable("Fido2PublicKeyCredentials");
            builder.HasOne<ApplicationUser>()
                   .WithMany(user => user.PublicKeyCredentials)
                   .HasForeignKey(credential => credential.UserId);
        }
    }
}
