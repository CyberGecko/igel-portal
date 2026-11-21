using System.Security.Claims;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using IgelPortal.Web.Pages.Hedgehogs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace IgelPortal.Web.Tests;

[TestClass]
public class ServiceRequestTests
{
    [TestMethod]
    [DataRow(false, false, false, true)]
    [DataRow(true, false, false, true)]
    [DataRow(false, true, true, true)]
    [DataRow(false, true, false, false)]
    public async Task ResponseLinkRequiresAnExistingRequestAndHedgehogInTheMembersGroup(
        bool deleted, bool member, bool hedgehogDeleted, bool sameGroup)
    {
        var request = new ServiceRequest { CommunityId = Guid.NewGuid(), HedgehogId = Guid.NewGuid(),
            DeletedDate = deleted ? DateTime.UnixEpoch : null };
        var groups = Substitute.For<ICommunityDataService>();
        var requests = Substitute.For<IServiceRequestDataService>();
        var hedgehogs = Substitute.For<IHedgehogDataService>();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "helper")], "test"))
        };
        requests.FindOneByToken(request.RequestToken).Returns(request);
        groups.IsCommunityMember(request.CommunityId, http.User.Identity).Returns(member);
        hedgehogs.FindOne(request.HedgehogId).Returns(new Hedgehog
        {
            CommunityId = sameGroup ? request.CommunityId : Guid.NewGuid(),
            DeletedDate = hedgehogDeleted ? DateTime.UnixEpoch : null
        });
        var page = new RequestServiceModel(hedgehogs, groups, requests, null!, null!, null!,
            NullLogger<RequestServiceModel>.Instance, null!)
        {
            PageContext = new PageContext { HttpContext = http }
        };

        var result = await page.OnGetServiceResponseAsync(request.RequestToken);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }
}
