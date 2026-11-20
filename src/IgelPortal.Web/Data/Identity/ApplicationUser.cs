using IgelPortal.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Data.Identity
{
    public class ApplicationUser : IdentityUser
    {
        [Display(Name = "First name")]
        [PersonalData]
        public string FirstName { get; set; } = default!;
        [Display(Name = "Last name")]
        [PersonalData]
        public string LastName { get; set; } = default!;

        public ICollection<PublicKeyCredential> PublicKeyCredentials { get; } = [];

        public override String ToString()
        {
            if (String.IsNullOrWhiteSpace(this.FirstName) || String.IsNullOrWhiteSpace(this.LastName))
            {
                return this.UserName ?? this.Email ?? "N.A.";
            }
            else
            {
                return $"{this.FirstName} {this.LastName}";
            }
        }
    }
}
