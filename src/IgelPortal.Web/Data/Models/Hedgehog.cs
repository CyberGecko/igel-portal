using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Data.Models
{
    public class Hedgehog : ModelBase
    {
        public String AttachedUser { get; set; } = default!;

        [Display(Name = "Attached User")]
        public String AttachedUserDisplayName { get; set; } = default!;

        [Display(Name = "BiologicalSex")]
        public BiologicalSex BiologicalSex { get; set; }

        [Display(Name = "Image")]
        public String? Image { get; set; }

        [Display(Name = "Community")]
        public Guid? CommunityId { get; set; }

        [Display(Name = "Home Territory")]
        public Guid? HomeTerritoryId { get; set; }
        [Display(Name = "Current Territory")]
        public Guid? CurrentTerritoryId { get; set; }
        [Display(Name = "Used Service")]
        public Guid? UsedServiceId { get; set; }

        [Display(Name = "Home Territory")]
        public String HomeTerritory { get; set; } = default!;
        [Display(Name = "Current Territory")]
        public String CurrentTerritory { get; set; } = default!;
        [Display(Name = "Community")]
        public String Community { get; set; } = default!;
        [Display(Name = "Used Service")]
        public String UsedService { get; set; } = default!;
        [Display(Name = "Open Service Requests")]
        public Int32 OpenServiceRequests { get; set; }

        public Boolean IsHome { get => this.HomeTerritoryId.HasValue && this.CurrentTerritoryId.HasValue && this.HomeTerritoryId == this.CurrentTerritoryId; }
    }
}
