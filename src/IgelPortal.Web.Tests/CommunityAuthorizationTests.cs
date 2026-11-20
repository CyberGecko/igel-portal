using System.Security.Claims;
using IgelPortal.Web.Interfaces;
using IgelPortal.Web.PolicyRequirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace IgelPortal.Web.Tests;

[TestClass]
public class CommunityAuthorizationTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "helper")], "test"));

    [TestMethod]
    [DataRow(CommunityRoles.OWNER, true)]
    [DataRow(CommunityRoles.MODERATOR, true)]
    [DataRow(CommunityRoles.MEMBER, false)]
    [DataRow(CommunityRoles.MEDICATION_PLANNER, false)]
    public async Task ManagementRequiresOwnerOrModeratorRoleInTheRequestedGroup(string role, bool allowed)
    {
        var target = Guid.NewGuid();
        var other = Guid.NewGuid();
        var groups = Substitute.For<ICommunityDataService>();
        groups.HasUserCommunityMemberRole(target, User.Identity, role).Returns(true);
        groups.HasUserCommunityMemberRole(other, User.Identity, CommunityRoles.OWNER).Returns(true);
        var requirement = new CommunityOwnerOrModeratorRequirement();
        var context = new AuthorizationHandlerContext([requirement], User, target);

        await new CommunityOwnerOrModeratorAuthorizationHandler(groups).HandleAsync(context);

        Assert.AreEqual(allowed, context.HasSucceeded);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task OwnerPolicyUsesTheRouteIdBeforeTheQueryString(bool isRouteOwner)
    {
        var target = Guid.NewGuid();
        var other = Guid.NewGuid();
        var groups = Substitute.For<ICommunityDataService>();
        groups.HasUserCommunityMemberRole(target, User.Identity, CommunityRoles.OWNER).Returns(isRouteOwner);
        groups.HasUserCommunityMemberRole(other, User.Identity, CommunityRoles.OWNER).Returns(true);
        var request = new DefaultHttpContext();
        request.Request.RouteValues["id"] = target;
        request.Request.QueryString = new QueryString("?id=" + other);
        var requirement = new CommunityRoleRequirement(CommunityRoles.OWNER);
        var context = new AuthorizationHandlerContext([requirement], User, request);

        await new CommunityRoleAuthorizationHandler(groups).HandleAsync(context);

        Assert.AreEqual(isRouteOwner, context.HasSucceeded);
        await groups.DidNotReceive().HasUserCommunityMemberRole(other, User.Identity, CommunityRoles.OWNER);
    }
}
