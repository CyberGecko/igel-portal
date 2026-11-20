using System.Security.Principal;
using Entity = IgelPortal.Web.Data.Entities;
using Model = IgelPortal.Web.Data.Models;

namespace IgelPortal.Web.Interfaces
{
    public interface ITerritoryDataService : IDataService<Entity.Territory, Model.Territory>
    {
        Task<IList<Model.Territory>> GetAllAdministratedByUser(IIdentity? identity);
        Task<Boolean> IsTerritoryDeletableByUser(Guid? territoryId, IIdentity? user);
        Task<IList<Model.Territory>> FindAllForCommunityWithService(Guid? communityId, Guid? serviceId);
        Task<IList<Model.Territory>> FindAllActiveForCommunity(Guid? communityId);
        Task<IList<Model.Territory>> FindAllActiveForUser(IIdentity? identity);
    }
}
