using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class AuthenticatorTransportMapping : IEntityTypeConfiguration<AuthenticatorTransport>
    {
        public void Configure(EntityTypeBuilder<AuthenticatorTransport> builder)
        {
            builder.ToTable("Fido2AuthenticatorTransports");
            builder.HasKey(transport => new { transport.PublicKeyCredentialId, transport.Value });
            builder.Property(c=>c.PublicKeyCredentialId).HasMaxLength(200);
        }
    }
}
