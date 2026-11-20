using AutoMapper;
using IgelPortal.Web.Data.Entities;
using IgelPortal.Web.Data.Models;

namespace IgelPortal.Web.Extensions
{
    public static class AutoMapperExtensions
    {
        public static IMappingExpression<TEntity, TModel> BaseMapping<TEntity, TModel>(this IMappingExpression<TEntity, TModel> mapper) where TEntity : EntityBase where TModel : ModelBase
        {
            return mapper.ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                         .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                         .ForMember(dest => dest.ChangeDate, opt => opt.MapFrom(src => src.ChangeDate))
                         .ForMember(dest => dest.CreationDate, opt => opt.MapFrom(src => src.CreationDate))
                         .ForMember(dest => dest.DeletedDate, opt => opt.MapFrom(src => src.DeletedDate));
        }
    }
}
