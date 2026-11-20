using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IgelPortal.Web.Data.Entities.Mappings
{
    public class CommunityMemberMapping : IEntityTypeConfiguration<CommunityMember>
    {
        public void Configure(EntityTypeBuilder<CommunityMember> builder)
        {
            builder.HasKey(e => new { e.UserName, e.CommunityId, e.CommunityRoleName });
            builder.Property(x => x.UserName).IsRequired();
            builder.Property(x => x.CommunityRoleName).IsRequired();

            builder.HasOne<Community>(e => e.Community).WithMany(e => e.CommunityMembers);
        }
    }
}
