namespace IgelPortal.Web.Data.Models.Mapping;

public static class TerritoryMapping
{
    public static Territory Map(Entities.Territory entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ChangeDate = entity.ChangeDate,
        CreationDate = entity.CreationDate,
        DeletedDate = entity.DeletedDate,
        Street = entity.Street,
        StreetNumber = entity.StreetNumber,
        PostalCode = entity.PostalCode,
        City = entity.City,
        UserName = entity.UserName!,
        CommunityId = entity.CommunityId,
        Longitude = entity.Longitude,
        Latitude = entity.Latitude,
        Community = (entity.Community?.Name)!,
        AttachedServices = entity.AttachedServices.Select(TerritoryAttachedServiceMapping.Map).ToList(),
        Hedgehogs = entity.HomeHedgehogs.Concat(entity.CurrentHedgehogs).DistinctBy(hedgehog => hedgehog.Id)
            .Select(hedgehog => new TerritoryHedgehog
            {
                Name = hedgehog.Name,
                IsHome = hedgehog.HomeTerritoryId.HasValue && hedgehog.HomeTerritoryId == hedgehog.CurrentTerritoryId,
                UsedService = hedgehog.UsedService?.TerritoryService?.Name,
                BiologicalSex = hedgehog.BiologicalSex
            }).ToList()
    };
}
