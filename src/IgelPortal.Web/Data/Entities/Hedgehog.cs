namespace IgelPortal.Web.Data.Entities
{
    public class Hedgehog : EntityBase
    {
        public Hedgehog()
        {
            this.ServiceRequests = [];
        }

        public String? AttachedUser { get; set; }
        public String? ImageString { get; set; }
        public BiologicalSex BiologicalSex { get; set; }

        public Guid? HomeTerritoryId { get; set; }
        public Guid? CurrentTerritoryId { get; set; }
        public Guid? CommunityId { get; set; }
        public Guid? UsedServiceId { get; set; }

        public virtual Territory? HomeTerritory { get; set; }
        public virtual Territory? CurrentTerritory { get; set; }
        public virtual Community? Community { get; set; }
        public virtual TerritoryAttachedService? UsedService { get; set; }
        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; }
    }

}
