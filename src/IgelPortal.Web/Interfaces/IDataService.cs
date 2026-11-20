using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Data.Entities;

namespace IgelPortal.Web.Interfaces
{
    public interface IDataService<TEntity, TModel> where TEntity : EntityBase where TModel : ModelBase
    {
        Task<Int32> Delete(Guid id);
        Task<IList<TModel>> FindAll();
        Task<IList<TModel>> Query(Func<TModel, bool> predicate);
        Task<TModel?> FindOne(Guid? id);
        Task<Guid> Insert(TModel model);
        Task<Int32> Update(TModel model);
    }
}
