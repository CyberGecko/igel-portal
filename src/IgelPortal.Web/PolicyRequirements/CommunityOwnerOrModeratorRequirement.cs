using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace IgelPortal.Web.PolicyRequirements;

public class CommunityOwnerOrModeratorRequirement : IAuthorizationRequirement;

public class CommunityOwnerOrModeratorAuthorizationHandler(ICommunityDataService communities)
    : AuthorizationHandler<CommunityOwnerOrModeratorRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CommunityOwnerOrModeratorRequirement requirement)
    {
        var communityId = context.Resource switch
        {
            Guid id => id,
            HttpContext httpContext when Guid.TryParse(
                httpContext.GetRouteValue("id")?.ToString() ?? httpContext.Request.Query["id"].ToString(), out var id) => id,
            _ => Guid.Empty
        };

        if (communityId != Guid.Empty &&
            (await communities.HasUserCommunityMemberRole(communityId, context.User.Identity, CommunityRoles.OWNER) ||
             await communities.HasUserCommunityMemberRole(communityId, context.User.Identity, CommunityRoles.MODERATOR)))
        {
            context.Succeed(requirement);
        }
    }
}
