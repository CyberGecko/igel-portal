using System.ComponentModel.DataAnnotations;

namespace IgelPortal.Web.Data.Models
{
    public class ModelBase
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(100)]
        [Display(Name = "Name")]
        public String Name { get; set; } = default!;

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
    }
}
