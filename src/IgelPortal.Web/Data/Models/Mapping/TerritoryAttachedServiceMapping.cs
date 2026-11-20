using AutoMapper;

namespace IgelPortal.Web.Data.Models.Mapping
{
    public class TerritoryAttachedServiceMapping : Profile
    {
        public TerritoryAttachedServiceMapping()
        {
            CreateMap<Entities.TerritoryAttachedService, TerritoryAttachedService>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.MaxCapacity, opt => opt.MapFrom(src => src.MaxCapacity))
                .ForMember(dest => dest.CurrentCapacity, opt => opt.MapFrom(src => src.MaxCapacity - src.Hedgehogs.Count))

                .ForMember(dest => dest.TerritoryId, opt => opt.MapFrom(src => src.TerritoryId))
                .ForMember(dest => dest.TerritoryServiceId, opt => opt.MapFrom(src => src.TerritoryServiceId))

                .ForMember(dest => dest.Territory, opt => opt.MapFrom((src,dst) => src.Territory?.Name))
                .ForMember(dest => dest.TerritoryService, opt => opt.MapFrom((src, dst) => src.TerritoryService?.Name));
        }
    }

}
