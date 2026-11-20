using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace IgelPortal.Web.PolicyRequirements
{
    public class CommunityOwnerOrModeratorRequirement() : IAuthorizationRequirement
    {
    }

    public class CommunityOwnerAuthorizationHandler(ICommunityDataService communityDataService) : AuthorizationHandler<CommunityOwnerOrModeratorRequirement>
    {
        private readonly ICommunityDataService communityDataService = communityDataService;

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CommunityOwnerOrModeratorRequirement requirement)
        {
            if (context.Resource is DefaultHttpContext httpContext && (Guid.TryParse(httpContext.GetRouteValue("id")?.ToString(), out Guid communityId) ||
                                                                       Guid.TryParse(httpContext?.Request?.QueryString.Value?.Remove(0, 4), out communityId)))
            {
                Boolean isOwner = await this.communityDataService.HasUserCommunityMemberRole(communityId, context.User.Identity, CommunityRoles.OWNER);

                if (isOwner)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }

    public class CommunityModeratorAuthorizationHandler(ICommunityDataService communityDataService) : AuthorizationHandler<CommunityOwnerOrModeratorRequirement>
    {
        private readonly ICommunityDataService communityDataService = communityDataService;

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CommunityOwnerOrModeratorRequirement requirement)
        {
            if (context.Resource is DefaultHttpContext httpContext && (Guid.TryParse(httpContext.GetRouteValue("id")?.ToString(), out Guid communityId) ||
                                                                       Guid.TryParse(httpContext?.Request?.QueryString.Value?.Remove(0, 4), out communityId)))
            {
                Boolean isOwner = await this.communityDataService.HasUserCommunityMemberRole(communityId, context.User.Identity, CommunityRoles.MODERATOR);

                if (isOwner)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
