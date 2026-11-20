using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Data.Models
{
    public class Territory : ModelBase
    {
        public Territory() : base()
        {
            this.Hedgehogs = [];
            this.AttachedServices = [];
        }

        [Display(Name = "Latitude")]
        public Double Latitude { get; set; }
        [Display(Name = "Longitude")]
        public Double Longitude { get; set; }
        [Required]
        [Display(Name = "Street")]
        public String Street { get; set; } = default!;
        [Required]
        [Display(Name = "StreetNumber")]
        public String StreetNumber { get; set; } = default!;
        [Required]
        [Display(Name = "Postal code")]
        public String PostalCode { get; set; } = default!;
        [Required]
        [Display(Name = "City")]
        public String City { get; set; } = default!;
        public String UserName { get; set; } = default!;

        [Display(Name = "Community")]
        public Guid? CommunityId { get; set; }
        [Display(Name = "Community")]
        public virtual String Community { get; set; } = default!;

        public virtual IList<TerritoryHedgehog> Hedgehogs { get; set; }
        [Display(Name = "Attached Services")]
        public virtual IList<TerritoryAttachedService> AttachedServices { get; set; }
    }

    public class TerritoryHedgehog
    {
        public String Name { get; set; } = default!;
        public Boolean IsHome { get; set; }
        public String? UsedService { get; set; }
        public BiologicalSex BiologicalSex { get; set; }
    }
    }
