namespace IgelPortal.Web.Data.Models
{
    public class PublicKeyCredential
    {
        public String Id { get; set; } = default!;
        public String UserId { get; set; } = default!;
        public IList<String> AuthenticationTransports { get; set; } = [];
        public IList<String> DevicePublicKeys { get; set; } = [];
    }
}
