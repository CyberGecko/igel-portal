using System.ComponentModel;

namespace IgelPortal.Web.Models
{
    public class UserAdministration
    {
        public UserAdministration()
        {
            this.Roles = [];
        }

        public String? Id { get; set; }
        [DisplayName("User Name")]
        public String? UserName { get; set; }
        [DisplayName("E-Mail")]
        public String? Email { get; set; }
        [DisplayName("E-Mail Confirmed")]
        public Boolean EmailConfirmed { get; set; }
        [DisplayName("2FA Enabled")]
        public Boolean TwoFactorEnabled { get; set; }
        [DisplayName("Failed Logins")]
        public Int32 AccessFailedCount { get; set; }
        [DisplayName("Assigned Roles")]
        public IList<String> Roles { get; internal set; }
    }
}
