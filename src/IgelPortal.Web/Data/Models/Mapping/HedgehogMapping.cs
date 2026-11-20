namespace IgelPortal.Web.Data.Models.Mapping;

public static class HedgehogMapping
{
    public static Hedgehog Map(Entities.Hedgehog entity, string? home, string? current, string? community,
        string? service, int openRequests, string? displayName) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ChangeDate = entity.ChangeDate,
        CreationDate = entity.CreationDate,
        DeletedDate = entity.DeletedDate,
        AttachedUser = entity.AttachedUser!,
        BiologicalSex = entity.BiologicalSex,
        CommunityId = entity.CommunityId,
        HomeTerritoryId = entity.HomeTerritoryId,
        CurrentTerritoryId = entity.CurrentTerritoryId,
        UsedServiceId = entity.UsedServiceId,
        AttachedUserDisplayName = (displayName ?? entity.AttachedUser)!,
        HomeTerritory = home!,
        CurrentTerritory = current!,
        Community = community!,
        UsedService = service!,
        Image = entity.ImageString,
        OpenServiceRequests = openRequests
    };
}
