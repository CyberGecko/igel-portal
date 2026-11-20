namespace IgelPortal.Web.Data.Entities
{
    public class ServiceRequestResponse
    {
        public Guid ServiceRequestId { get; set; }
        public String RespondingUserName { get; set; } = default!;
        public DateTime ResponseDate { get; set; }
    }
}