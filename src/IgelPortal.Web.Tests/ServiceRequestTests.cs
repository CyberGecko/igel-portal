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
    [DataRow(false, false, true, true)]
    [DataRow(false, false, false, false)]
    [DataRow(true, false, true, false)]
    [DataRow(false, true, true, false)]
    public async Task OnlyActiveRequestsCanReceiveResponsesFromGroupMembers(bool deleted, bool fulfilled, bool member, bool accepted)
    {
        var request = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            CommunityId = Guid.NewGuid(),
            DeletedDate = deleted ? DateTime.UnixEpoch : null,
            FulfillmentDate = fulfilled ? DateTime.UnixEpoch : null
        };
        var groups = Substitute.For<ICommunityDataService>();
        var requests = Substitute.For<IServiceRequestDataService>();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "helper")], "test"))
        };
        requests.FindOneByToken(request.RequestToken).Returns(request);
        groups.IsCommunityMember(request.CommunityId, http.User.Identity).Returns(member);

        // Other page handlers use those services. This path only fetches the request, checks membership, and records the response.
        var page = new RequestServiceModel(null!, groups, null!, null!, requests, null!, null!, null!, null!,
            NullLogger<RequestServiceModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = http }
        };

        var result = await page.OnPostServiceResponse(request.RequestToken);

        if (accepted)
        {
            Assert.IsInstanceOfType<RedirectToPageResult>(result);
            await requests.Received(1).AddResponseToRequest(request.Id, http.User.Identity);
        }
        else
        {
            Assert.IsInstanceOfType<NotFoundResult>(result);
            await requests.DidNotReceiveWithAnyArgs().AddResponseToRequest(default, default);
        }
    }
}
