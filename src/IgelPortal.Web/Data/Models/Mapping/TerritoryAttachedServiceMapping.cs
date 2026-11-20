namespace IgelPortal.Web.Data.Models.Mapping;

public static class TerritoryAttachedServiceMapping
{
    public static TerritoryAttachedService Map(Entities.TerritoryAttachedService entity) => new()
    {
        Id = entity.Id,
        MaxCapacity = entity.MaxCapacity,
        TerritoryId = entity.TerritoryId,
        TerritoryServiceId = entity.TerritoryServiceId,
        CurrentCapacity = entity.MaxCapacity - entity.Hedgehogs.Count,
        Territory = (entity.Territory?.Name)!,
        TerritoryService = (entity.TerritoryService?.Name)!
    };
}
