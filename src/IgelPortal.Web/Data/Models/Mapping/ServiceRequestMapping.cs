namespace IgelPortal.Web.Data.Models.Mapping;

public static class ServiceRequestMapping
{
    public static ServiceRequest Map(Entities.ServiceRequest entity) => new()
    {
        Id = entity.Id,
        ServiceId = entity.ServiceId,
        HedgehogId = entity.HedgehogId,
        CommunityId = entity.CommunityId,
        FulfillmentDate = entity.FulfillmentDate,
        NeededFrom = entity.NeededFrom,
        NeededUntil = entity.NeededUntil,
        RequestNote = entity.RequestNote,
        AssignedTerritoryServiceId = entity.AssignedTerritoryServiceId,
        AssignedTerritoryName = entity.AssignedTerritoryName,
        AssignedUserName = entity.AssignedUserName,
        CreationDate = entity.CreationDate,
        ChangeDate = entity.ChangeDate,
        DeletedDate = entity.DeletedDate,
        RequestToken = entity.RequestToken,
        Service = (entity.Service?.Name)!,
        Hedgehog = (entity.Hedgehog?.Name)!,
        Responses = entity.Responses.Select(ServiceRequestResponseMapping.Map).ToList()
    };
}
