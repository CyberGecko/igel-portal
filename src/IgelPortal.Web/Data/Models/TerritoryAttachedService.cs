using IgelPortal.Web.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Data.Models
{
    public class TerritoryAttachedService
    {
        public Guid Id { get; set; }
        [Display(Name = "Territory")]
        public Guid TerritoryId { get; set; }
        [Display(Name = "Service")]
        public Guid TerritoryServiceId { get; set; }

        [Range(0, Double.PositiveInfinity)]
        [Display(Name = "Max. Capacity")]
        public Int32 MaxCapacity { get; set; }

        [Display(Name = "Current Capacity")]
        public Int32 CurrentCapacity { get; internal init; }

        [Display(Name = "Territory")]
        public String Territory { get; set; } = default!;
        [Display(Name = "Service")]
        public String TerritoryService { get; set; } = default!;
    }
}
