using Model = IgelPortal.Web.Data.Models;

namespace IgelPortal.Web.Interfaces
{
    public interface ITerritoryAttachedServiceDataService
    {
        Task<Int32> Delete(Guid id);
        Task<IList<Model.TerritoryAttachedService>> FindAll();
        Task<IList<Model.TerritoryAttachedService>> Query(Func<Model.TerritoryAttachedService, bool> predicate);
        Task<Model.TerritoryAttachedService?> FindOne(Guid id);
        Task<Int32> Insert(Model.TerritoryAttachedService model);
        Task<Int32> Update(Model.TerritoryAttachedService model);
    }
}
