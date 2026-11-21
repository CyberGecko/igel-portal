namespace IgelPortal.Web.Data.Models.Mapping;

public static class ServiceRequestResponseMapping
{
    public static ServiceRequestResponse Map(Entities.ServiceRequestResponse entity) => new()
    {
        ServiceRequestId = entity.ServiceRequestId,
        RespondingUserName = entity.RespondingUserName,
        ResponseDate = entity.ResponseDate,
        IsAvailable = entity.IsAvailable,
        OfferedTerritoryServiceId = entity.OfferedTerritoryServiceId,
        RespondingUserDisplayName = entity.RespondingUserName
    };
}
