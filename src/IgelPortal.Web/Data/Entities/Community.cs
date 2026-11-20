namespace IgelPortal.Web.Data.Entities
{
    public class Community : EntityBase
    {
        public Community()
        {
            this.Hedgehogs = [];
            this.Territories = [];
            this.CommunityMembers = [];
            this.ServiceRequests = [];
        }

        public String? UserName { get; set; }
        public String? Description { get; set; }
        public String? ContactName { get; set; }
        public String? ContactMail { get; set; }
        public String? ContactPhone { get; set; }

        public virtual ICollection<Hedgehog> Hedgehogs { get; set; }
        public virtual ICollection<Territory> Territories { get; set; }
        public virtual ICollection<CommunityMember> CommunityMembers { get; set; }
        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; }
    }

}
