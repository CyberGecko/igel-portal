namespace IgelPortal.Web.Data.Models.Mapping;

public static class CommunityMapping
{
    public static Community Map(Entities.Community entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ChangeDate = entity.ChangeDate,
        CreationDate = entity.CreationDate,
        DeletedDate = entity.DeletedDate,
        ContactName = entity.ContactName,
        ContactMail = entity.ContactMail!,
        ContactPhone = entity.ContactPhone,
        Description = entity.Description,
        UserName = entity.UserName
    };
}
