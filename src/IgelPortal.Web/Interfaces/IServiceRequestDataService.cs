using IgelPortal.Web.Data.Models;

namespace IgelPortal.Web.Interfaces
{
    public interface IServiceRequestDataService
    {
        Task<Int32> Delete(Guid id);
        Task<IList<ServiceRequest>> FindAll();
        Task<IList<ServiceRequest>> Query(Func<ServiceRequest, bool> predicate);
        Task<ServiceRequest?> FindOne(Guid? id);
        Task<Guid> Insert(ServiceRequest model);
        Task<Int32> Update(ServiceRequest model);
        Task<ServiceRequest?> FindOneByToken(Guid? requestToken);
        Task<IList<ServiceRequest>> FindAllActiveForCommunityAndHedgehog(Guid? communityId, Guid? hedgehogId);
    }
}
