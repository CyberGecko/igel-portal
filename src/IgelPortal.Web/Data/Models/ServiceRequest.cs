using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Data.Models
{
    public class ServiceRequest
    {
        public ServiceRequest()
        {
            this.RequestToken = Guid.NewGuid();
        }

        public Guid Id { get; set; }
        public Guid CommunityId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid HedgehogId { get; set; }
        public Guid RequestToken { get; set; }
        [DataType(DataType.DateTime)]
        [Display(Name = "Fulfillment date")]
        public DateTime? FulfillmentDate { get; set; }
        [DataType(DataType.DateTime)]
        [Display(Name = "Creation date")]
        public DateTime CreationDate { get; set; }
        [DataType(DataType.DateTime)]
        [Display(Name = "Change date")]
        public DateTime ChangeDate { get; set; }
        [DataType(DataType.DateTime)]
        [Display(Name = "Deleted date")]
        public DateTime? DeletedDate { get; set; }

        [Display(Name = "Is deleted")]
        public Boolean IsDeleted { get => this.DeletedDate.HasValue; }
        [Display(Name = "Is Fulfilled")]
        public Boolean IsFulfilled { get => this.FulfillmentDate.HasValue; }
        [Display(Name = "Service")]
        public String Service { get; set; } = default!;
        [Display(Name = "Hedgehog")]
        public String Hedgehog { get; set; } = default!;

        [Display(Name = "Responses")]
        public IList<ServiceRequestResponse> Responses { get; set; } = default!;

    }
}