using System.ComponentModel;

namespace IgelPortal.Web.Models
{
    public class RoleAdministration
    {
        public String? Id { get; set; }
        [DisplayName("Role Name")]
        public String? Name { get; set; }
        [DisplayName("Assigned Users")]
        public int UsersCount { get; internal set; }
    }
}
