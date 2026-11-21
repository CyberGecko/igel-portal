namespace IgelPortal.Web.Data.Models.Mapping;

public static class TerritoryAttachedServiceMapping
{
    public static TerritoryAttachedService Map(Entities.TerritoryAttachedService entity) => new()
    {
        Id = entity.Id,
        MaxCapacity = entity.MaxCapacity,
        IsPaused = entity.IsPaused,
        TerritoryId = entity.TerritoryId,
        TerritoryServiceId = entity.TerritoryServiceId,
        CurrentCapacity = Math.Max(0, entity.MaxCapacity - entity.Hedgehogs.Count(hedgehog => hedgehog.DeletedDate == null)),
        Territory = (entity.Territory?.Name)!,
        TerritoryService = (entity.TerritoryService?.Name)!
    };
}
