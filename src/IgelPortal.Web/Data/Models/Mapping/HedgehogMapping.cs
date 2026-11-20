using AutoMapper;
using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Extensions;
using Microsoft.AspNetCore.Identity;

namespace IgelPortal.Web.Data.Models.Mapping
{
    public class HedgehogMapping : Profile
    {
        private readonly UserManager<ApplicationUser> userManager;

        public HedgehogMapping(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

            CreateMap<Entities.Hedgehog, Hedgehog>()
                .BaseMapping()

                .ForMember(dest => dest.AttachedUser, opt => opt.MapFrom(src => src.AttachedUser))
                .ForMember(dest => dest.BiologicalSex, opt => opt.MapFrom(src => src.BiologicalSex))

                .ForMember(dest => dest.CommunityId, opt => opt.MapFrom(src => src.CommunityId))
                .ForMember(dest => dest.HomeTerritoryId, opt => opt.MapFrom(src => src.HomeTerritoryId))
                .ForMember(dest => dest.CurrentTerritoryId, opt => opt.MapFrom(src => src.CurrentTerritoryId))
                .ForMember(dest => dest.UsedServiceId, opt => opt.MapFrom(src => src.UsedServiceId))

                .ForMember(dest => dest.HomeTerritory, opt => opt.MapFrom((src, dst) => src.HomeTerritory?.Name))
                .ForMember(dest => dest.CurrentTerritory, opt => opt.MapFrom((src, dst) => src.CurrentTerritory?.Name))
                .ForMember(dest => dest.Community, opt => opt.MapFrom((src, dst) => src.Community?.Name))
                .ForMember(dest => dest.UsedService, opt => opt.MapFrom((src, dst) => src.UsedService?.TerritoryService?.Name))

                .ForMember(dest => dest.AttachedUserDisplayName, opt => opt.MapFrom(src => this.GetUserDisplayName(src)))
                .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.ImageString))
                .ForMember(dest => dest.OpenServiceRequests, opt => opt.MapFrom(src => src.ServiceRequests.Where(c => !c.FulfillmentDate.HasValue && !c.DeletedDate.HasValue).Count()));
        }

        private String? GetUserDisplayName(Entities.Hedgehog entity)
        {
            return this.userManager.Users.FirstOrDefault(c => c.UserName == entity.AttachedUser)?.ToString() ?? entity.AttachedUser;
        }
    }
}
