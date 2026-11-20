using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Models
{
    public class CommunityMembersModel
    {
        [Display(Name = "User Name")]
        public String UserName { get; set; } = default!;
        [Display(Name = "Community")]
        public Guid CommunityId { get; set; }
        [Display(Name ="Community Roles")]
        public IList<String> CommunityRoles { get; set; } = default!;
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = default!;
    }
}
