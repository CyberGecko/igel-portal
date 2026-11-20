namespace IgelPortal.Web.Data.Entities
{
    public class TerritoryAttachedService
    {
        public TerritoryAttachedService()
        {
            this.Hedgehogs = [];
        }

        public Guid Id { get; set; }
        public Guid TerritoryId { get; set; }
        public Guid TerritoryServiceId { get; set; }

        public Int32 MaxCapacity { get; set; }

        public virtual Territory? Territory { get; set; }
        public virtual TerritoryService? TerritoryService { get; set; }
        public virtual ICollection<Hedgehog> Hedgehogs { get; set; }
    }

}
