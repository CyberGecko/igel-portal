using AutoMapper;
using IgelPortal.Web.Extensions;

namespace IgelPortal.Web.Data.Models.Mapping
{
    public class TerritoryMapping : Profile
    {

        public TerritoryMapping()
        {
            CreateMap<Entities.Territory, Territory>()
                .BaseMapping()

                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street))
                .ForMember(dest => dest.StreetNumber, opt => opt.MapFrom(src => src.StreetNumber))
                .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.PostalCode))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))

                .ForMember(dest => dest.CommunityId, opt => opt.MapFrom(src => src.CommunityId))

                .ForMember(dest => dest.Hedgehogs, opt => opt.MapFrom(src => src.HomeHedgehogs.Concat(src.CurrentHedgehogs)
                                                                                              .DistinctBy(e => e.Id)
                                                                                              .Select(this.MapHedgehogValue)))
                .ForMember(dest => dest.Community, opt => opt.MapFrom((src, dst) => src.Community?.Name))
                .ForMember(dest => dest.AttachedServices, opt => opt.MapFrom(src => src.AttachedServices))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude));
        }

        private TerritoryHedgehog MapHedgehogValue(Entities.Hedgehog e)
        {
            return new TerritoryHedgehog()
            {
                Name = e.Name,
                IsHome = e.HomeTerritoryId.HasValue && e.CurrentTerritoryId.HasValue && e.HomeTerritoryId == e.CurrentTerritoryId,
                UsedService = e.UsedService?.TerritoryService?.Name,
                BiologicalSex = e.BiologicalSex,
            };
        }
    }
}
