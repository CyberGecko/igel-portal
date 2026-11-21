using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Data.Models
{
    public class ServiceRequestResponse
    {
        public Guid ServiceRequestId { get; set; }
        [Display(Name = "Username")]
        public String RespondingUserName { get; set; } = default!;
        [Display(Name = "Username")]
        public String RespondingUserDisplayName { get; set; } = default!;
        [DataType(DataType.DateTime)]
        [Display(Name = "Response date")]
        public DateTime ResponseDate { get; set; }
        public bool IsAvailable { get; set; } = true;
        public Guid? OfferedTerritoryServiceId { get; set; }
    }
}