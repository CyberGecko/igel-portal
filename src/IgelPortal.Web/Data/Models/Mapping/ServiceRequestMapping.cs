using AutoMapper;

namespace IgelPortal.Web.Data.Models.Mapping
{
    public class ServiceRequestMapping : Profile
    {
        public ServiceRequestMapping()
        {
            CreateMap<Entities.ServiceRequest, ServiceRequest>()

                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ServiceId, opt => opt.MapFrom(src => src.ServiceId))
                .ForMember(dest => dest.HedgehogId, opt => opt.MapFrom(src => src.HedgehogId))
                .ForMember(dest => dest.CommunityId, opt => opt.MapFrom(src => src.CommunityId))
                .ForMember(dest => dest.FulfillmentDate, opt => opt.MapFrom(src => src.FulfillmentDate))
                .ForMember(dest => dest.CreationDate, opt => opt.MapFrom(src => src.CreationDate))
                .ForMember(dest => dest.ChangeDate, opt => opt.MapFrom(src => src.ChangeDate))
                .ForMember(dest => dest.RequestToken, opt => opt.MapFrom(src => src.RequestToken))

                .ForMember(dest => dest.Service, opt => opt.MapFrom(src => src.Service.Name))
                .ForMember(dest => dest.Hedgehog, opt => opt.MapFrom(src => src.Hedgehog.Name))
                .ForMember(dest => dest.Responses, opt => opt.MapFrom(src => src.Responses));
        }
    }
}
