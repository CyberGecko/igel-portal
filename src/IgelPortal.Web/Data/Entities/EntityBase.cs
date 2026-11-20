namespace IgelPortal.Web.Data.Entities
{
    public class EntityBase
    {
        public Guid Id { get; set; }
        public String Name { get; set; } = "";
        public DateTime CreationDate { get; set; }
        public DateTime ChangeDate { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
