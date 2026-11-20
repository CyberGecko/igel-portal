using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class DevicePublicKeyMapping : IEntityTypeConfiguration<DevicePublicKey>
    {
        public void Configure(EntityTypeBuilder<DevicePublicKey> builder)
        {
            builder.ToTable("Fido2DevicePublicKeys");
            builder.HasKey(key => new { key.PublicKeyCredentialId, key.Value }).HasName("FK_Fido2DevicePubKeys_PubKeyCreds");

            builder.Property(c => c.PublicKeyCredentialId).HasMaxLength(255);
            builder.Property(c => c.Value).HasMaxLength(1024);
        }
    }
}
