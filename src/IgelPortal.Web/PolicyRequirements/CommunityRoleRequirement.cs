using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace IgelPortal.Web.PolicyRequirements
{
    public class CommunityRoleRequirement(String communityRole) : IAuthorizationRequirement
    {
        public String CommunityRole { get; } = communityRole;
    }

    public class CommunityRoleAuthorizationHandler(ICommunityDataService communityDataService) : AuthorizationHandler<CommunityRoleRequirement>
    {
        private readonly ICommunityDataService communityDataService = communityDataService;

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CommunityRoleRequirement requirement)
        {
            if (context.Resource is DefaultHttpContext httpContext && Guid.TryParse(httpContext.GetRouteValue("id")?.ToString(), out Guid communityId) && !String.IsNullOrEmpty(requirement.CommunityRole))
            {
                Boolean isOwner = await this.communityDataService.HasUserCommunityMemberRole(communityId, context.User.Identity, requirement.CommunityRole);

                if (isOwner)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail(new AuthorizationFailureReason(this, "You are not authorized to access this resource."));
                }
            }
            else
            {
                context.Fail();
            }
        }
    }
    
}
