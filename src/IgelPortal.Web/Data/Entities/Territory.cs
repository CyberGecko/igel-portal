using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Data.Entities
{
    public class Territory : EntityBase
    {
        public Territory()
        {
            this.HomeHedgehogs = [];
            this.CurrentHedgehogs = [];
            this.AttachedServices = [];
        }

        public String Street { get; set; } = default!;
        public String StreetNumber { get; set; } = default!;
        public String PostalCode { get; set; } = default!;
        public String City { get; set; } = default!;
        public Guid? CommunityId { get; set; }
        public String? UserName { get; set; }
        public Double Latitude { get; set; }
        public Double Longitude { get; set; }

        public virtual ICollection<Hedgehog> HomeHedgehogs { get; set; }
        public virtual ICollection<Hedgehog> CurrentHedgehogs { get; set; }
        public virtual Community? Community { get; set; }
        public virtual ICollection<TerritoryAttachedService> AttachedServices { get; set; }
    }
}
