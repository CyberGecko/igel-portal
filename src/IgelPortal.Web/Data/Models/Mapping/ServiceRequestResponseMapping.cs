using AutoMapper;
using IgelPortal.Web.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace IgelPortal.Web.Data.Models.Mapping
{
    public class ServiceRequestResponseMapping : Profile
    {
        private readonly UserManager<ApplicationUser> userManager;
        public ServiceRequestResponseMapping(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;

            CreateMap<Entities.ServiceRequestResponse, ServiceRequestResponse>()

                .ForMember(dest => dest.ServiceRequestId, opt => opt.MapFrom(src => src.ServiceRequestId))
                .ForMember(dest => dest.RespondingUserName, opt => opt.MapFrom(src => src.RespondingUserName))
                .ForMember(dest => dest.RespondingUserDisplayName, opt => opt.MapFrom(src => this.GetUserDisplayName(src)))
                .ForMember(dest => dest.ResponseDate, opt => opt.MapFrom(src => src.ResponseDate));
        }

        private String? GetUserDisplayName(Entities.ServiceRequestResponse entity)
        {
            return this.userManager.Users.FirstOrDefault(c => c.UserName == entity.RespondingUserName)?.ToString() ?? entity.RespondingUserName;
        }
    }
}
