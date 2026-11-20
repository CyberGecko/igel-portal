namespace IgelPortal.Web.Data.Models
{
    public class CommunityMember
    {
        public String UserName { get; set; } = default!;
        public String CommunityRoleName { get; set; } = default!;
        public Guid CommunityId { get; set; }
        public String DisplayName { get; set; } = default!;

        public String Community { get; set; } = default!;
    }
}
