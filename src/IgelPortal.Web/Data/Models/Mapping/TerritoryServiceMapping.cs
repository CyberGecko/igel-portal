namespace IgelPortal.Web.Data.Models.Mapping;

public static class TerritoryServiceMapping
{
    public static TerritoryService Map(Entities.TerritoryService entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ChangeDate = entity.ChangeDate,
        CreationDate = entity.CreationDate,
        DeletedDate = entity.DeletedDate
    };
}
