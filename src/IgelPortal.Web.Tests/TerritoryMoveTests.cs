using System.Security.Claims;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using IgelPortal.Web.Pages.Hedgehogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace IgelPortal.Web.Tests;

[TestClass]
public class TerritoryMoveTests
{
    [TestMethod]
    [DataRow(true, 1, true)]
    [DataRow(true, 0, false)]
    [DataRow(false, 1, false)]
    public async Task MoveToAServiceRequiresCapacityAndATerritoryInTheSameGroup(bool sameGroup, int capacity, bool allowed)
    {
        var groupId = Guid.NewGuid();
        var hedgehog = new Hedgehog { Id = Guid.NewGuid(), CommunityId = groupId, HomeTerritoryId = Guid.NewGuid() };
        hedgehog.CurrentTerritoryId = hedgehog.HomeTerritoryId;
        var service = new TerritoryAttachedService { Id = Guid.NewGuid(), CurrentCapacity = capacity };
        var territory = new Territory
        {
            Id = Guid.NewGuid(),
            CommunityId = sameGroup ? groupId : Guid.NewGuid(),
            AttachedServices = [service]
        };
        var hedgehogs = Substitute.For<IHedgehogDataService>();
        var territories = Substitute.For<ITerritoryDataService>();
        var authorization = Substitute.For<IAuthorizationService>();
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("test")) };
        hedgehogs.FindOne(hedgehog.Id).Returns(hedgehog);
        territories.FindOne(territory.Id).Returns(territory);
        authorization.AuthorizeAsync(http.User, groupId, PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)
            .Returns(AuthorizationResult.Success());
        var page = new MoveTerritoryModel(hedgehogs, territories, authorization)
        {
            PageContext = new PageContext { HttpContext = http }
        };

        var result = await page.OnPost(hedgehog.Id, territory.Id, service.Id, groupId);

        if (allowed)
        {
            Assert.IsInstanceOfType<RedirectToPageResult>(result);
            Assert.AreEqual(territory.Id, hedgehog.CurrentTerritoryId);
            Assert.AreEqual(service.Id, hedgehog.UsedServiceId);
            await hedgehogs.Received(1).Update(hedgehog);
        }
        else
        {
            Assert.IsInstanceOfType<BadRequestResult>(result);
            Assert.AreEqual(hedgehog.HomeTerritoryId, hedgehog.CurrentTerritoryId);
            Assert.IsNull(hedgehog.UsedServiceId);
            await hedgehogs.DidNotReceive().Update(Arg.Any<Hedgehog>());
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ReturningHomeAndReleasingAServiceRequiresAnExplicitAction(bool returnHome)
    {
        var groupId = Guid.NewGuid();
        var home = new Territory { Id = Guid.NewGuid(), CommunityId = groupId };
        var previousTerritory = Guid.NewGuid();
        var previousService = Guid.NewGuid();
        var hedgehog = new Hedgehog
        {
            Id = Guid.NewGuid(),
            CommunityId = groupId,
            HomeTerritoryId = home.Id,
            CurrentTerritoryId = previousTerritory,
            UsedServiceId = previousService
        };
        var hedgehogs = Substitute.For<IHedgehogDataService>();
        var territories = Substitute.For<ITerritoryDataService>();
        var authorization = Substitute.For<IAuthorizationService>();
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("test")) };
        hedgehogs.FindOne(hedgehog.Id).Returns(hedgehog);
        territories.FindOne(home.Id).Returns(home);
        authorization.AuthorizeAsync(http.User, groupId, PolicyNames.COMMUNITY_OWNER_OR_MODERATOR)
            .Returns(AuthorizationResult.Success());
        var page = new MoveTerritoryModel(hedgehogs, territories, authorization)
        {
            PageContext = new PageContext { HttpContext = http }
        };

        if (returnHome)
        {
            Assert.IsInstanceOfType<RedirectToPageResult>(await page.OnPostReturnHomeAsync(hedgehog.Id, groupId));
            Assert.AreEqual(home.Id, hedgehog.CurrentTerritoryId);
            Assert.IsNull(hedgehog.UsedServiceId);
            await hedgehogs.Received(1).Update(hedgehog);
        }
        else
        {
            Assert.IsInstanceOfType<BadRequestResult>(await page.OnPost(hedgehog.Id, home.Id, null, groupId));
            Assert.AreEqual(previousTerritory, hedgehog.CurrentTerritoryId);
            Assert.AreEqual(previousService, hedgehog.UsedServiceId);
            await hedgehogs.DidNotReceive().Update(Arg.Any<Hedgehog>());
        }
    }
}
