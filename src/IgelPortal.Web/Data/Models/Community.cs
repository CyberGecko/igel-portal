using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Data.Models
{
    public class Community : ModelBase
    {
        public Community() : base()
        {
        }

        public String? UserName { get; set; }
        [DataType(DataType.MultilineText)]
        [MaxLength(255)]
        [DisplayName("Description")]
        public String? Description { get; set; }
        [Required]
        [DisplayName("Contact person")]
        public String? ContactName { get; set; }
        [Required]
        [DisplayName("E-Mail")]
        [DataType(DataType.EmailAddress)]
        public String ContactMail { get; set; } = default!;
        [DisplayName("Phone")]
        [DataType(DataType.PhoneNumber)]
        public String? ContactPhone { get; set; }

    }
}
