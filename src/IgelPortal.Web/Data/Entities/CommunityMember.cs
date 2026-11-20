namespace IgelPortal.Web.Data.Entities
{
    public class CommunityMember
    {
        public String? UserName { get; set; }
        public String? CommunityRoleName { get; set; }
        public Guid CommunityId { get; set; }

        public virtual Community Community { get; set; } = default!;
    }
}
