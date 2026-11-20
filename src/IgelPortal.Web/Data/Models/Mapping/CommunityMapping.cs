using AutoMapper;
using IgelPortal.Web.Extensions;

namespace IgelPortal.Web.Data.Models.Mapping
{
    public class CommunityMapping : Profile
    {
        public CommunityMapping()
        {
            CreateMap<Entities.Community, Community>()
                .BaseMapping()

                .ForMember(dest => dest.ContactName, opt => opt.MapFrom(src => src.ContactName))
                .ForMember(dest => dest.ContactMail, opt => opt.MapFrom(src => src.ContactMail))
                .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactPhone))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName));
        }
    }
}
