using AutoMapper;
using IgelPortal.Web.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace IgelPortal.Web.Data.Models.Mapping
{
    public class CommunityMemberMapping : Profile
    {
        private readonly UserManager<ApplicationUser> userManager;
        public CommunityMemberMapping(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

            CreateMap<Entities.CommunityMember, CommunityMember>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.CommunityId, opt => opt.MapFrom(src => src.CommunityId))
                .ForMember(dest => dest.CommunityRoleName, opt => opt.MapFrom(src => src.CommunityRoleName))

                .ForMember(dest => dest.Community, opt => opt.MapFrom(src => src.Community.Name))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => this.GetUserDisplayName(src)));
        }

        private String? GetUserDisplayName(Entities.CommunityMember entity)
        {
            return this.userManager.Users.FirstOrDefault(c => c.UserName == entity.UserName)?.ToString() ?? entity.UserName;
        }
    }
}
