using Model = IgelPortal.Web.Data.Models;
using Entity = IgelPortal.Web.Data.Entities;

namespace IgelPortal.Web.Interfaces
{
    public interface IHedgehogDataService : IDataService<Entity.Hedgehog, Model.Hedgehog>
    {
        Task<IList<Model.Hedgehog>> FindAllActiveForCommunity(Guid? communityId);
    }
}
