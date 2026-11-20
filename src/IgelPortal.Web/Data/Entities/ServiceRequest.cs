namespace IgelPortal.Web.Data.Entities
{
    public class ServiceRequest
    {
        public Guid Id { get; set; }
        public Guid CommunityId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid HedgehogId { get; set; }
        public Guid RequestToken { get; set; }
        public DateTime? FulfillmentDate { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ChangeDate { get; set; }
        public DateTime? DeletedDate { get; set; }

        public virtual TerritoryService Service { get; set; } = default!;
        public virtual Hedgehog Hedgehog { get; set; } = default!;
        public virtual Community Community { get; set; } = default!;
        public virtual IList<ServiceRequestResponse> Responses { get; set; } = default!;
    }
}