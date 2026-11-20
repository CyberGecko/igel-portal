using System.Globalization;
using System.Security.Claims;
using IgelPortal.Web.Data;
using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace IgelPortal.Web.Tests;

[TestClass]
public class RecentAuthenticationTests
{
    private const string TimestampKey = "igel.authenticated-at";

    [TestMethod]
    [DataRow(-301, false)]
    [DataRow(-300, true)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    public async Task ConfirmationRequiresATimestampWithinTheLastFiveMinutes(int seconds, bool expected)
    {
        using var request = RequestWithTimestamp(Timestamp(seconds));

        Assert.AreEqual(expected, await PortalSignInManager.HasRecentAuthenticationAsync(request.Http));
        Assert.AreEqual(IdentityConstants.ApplicationScheme, request.Authentication.RequestedScheme);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("invalid")]
    public async Task MissingOrInvalidAuthenticationTimestampDoesNotConfirmAccount(string? timestamp)
    {
        using var request = RequestWithTimestamp(timestamp);

        Assert.IsFalse(await PortalSignInManager.HasRecentAuthenticationAsync(request.Http));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task SigningInStampsFreshAuthenticationWithoutRenewingExistingConfirmation(bool refresh, bool hasOldTimestamp)
    {
        var user = new ApplicationUser();
        var properties = new AuthenticationProperties { IssuedUtc = AuthenticationTestContext.Now.AddMinutes(-1) };
        if (hasOldTimestamp)
        {
            properties.Items.Add(TimestampKey, Timestamp(-600));
        }

        var result = AuthenticationTestContext.Authenticated(IdentityConstants.ApplicationScheme, properties,
            new Claim(ClaimTypes.NameIdentifier, user.Id));
        using var request = new AuthenticationTestContext(result, services =>
        {
            services.AddLogging();
            services.AddAuthentication();
            services.AddHttpContextAccessor();
            services.AddDbContext<ApplicationDbContext>();
            services.AddIdentityCore<ApplicationUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager<PortalSignInManager>();
            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, TestPrincipalFactory>();
        });
        using var scope = request.Http.RequestServices.CreateScope();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        signInManager.Context = request.Http;

        if (refresh)
        {
            await signInManager.RefreshSignInAsync(user);
        }
        else
        {
            await signInManager.SignInAsync(user, isPersistent: false);
        }

        Assert.IsNotNull(request.Authentication.SignInProperties);
        request.Authentication.SignInProperties.Items.TryGetValue(TimestampKey, out var actualTimestamp);
        var expected = refresh ? (hasOldTimestamp ? Timestamp(-600) : null) : Timestamp(0);
        Assert.AreEqual(expected, actualTimestamp);
    }

    [TestMethod]
    [DataRow("GET", null, false, false, true)]
    [DataRow("GET", null, true, false, false)]
    [DataRow("GET", "LinkLoginCallback", false, false, false)]
    [DataRow("POST", null, false, false, false)]
    [DataRow("POST", null, false, true, true)]
    public async Task SensitiveHandlersRequireFreshAuthentication(
        string method, string? handlerName, bool protectGet, bool fresh, bool shouldRun)
    {
        using var request = RequestWithTimestamp(Timestamp(fresh ? -1 : -301));
        request.Http.Request.Method = method;
        request.Http.Request.PathBase = "/portal";
        request.Http.Request.Path = "/Identity/Account/Manage/PasskeyAuthentication";
        request.Http.Request.QueryString = new QueryString("?handler=Remove");
        var page = new PageContext(new ActionContext(request.Http, new RouteData(), new CompiledPageActionDescriptor()));
        var handler = handlerName is null ? null : new HandlerMethodDescriptor { Name = handlerName };
        var instance = new object();
        var context = new PageHandlerExecutingContext(page, [], handler, new Dictionary<string, object?>(), instance);
        var filter = new RecentAuthenticationFilter(protectGet);
        var executed = false;

        await filter.OnPageHandlerExecutionAsync(context, () =>
        {
            executed = true;
            return Task.FromResult(new PageHandlerExecutedContext(page, [], handler, instance));
        });

        Assert.AreEqual(shouldRun, executed);
        if (!shouldRun)
        {
            var redirect = (RedirectToPageResult)context.Result!;
            Assert.AreEqual("/Account/Login", redirect.PageName);
            Assert.AreEqual("Identity", redirect.RouteValues!["area"]);
            Assert.AreEqual(true, redirect.RouteValues["confirmAccount"]);
            Assert.AreEqual("/portal/Identity/Account/Manage/PasskeyAuthentication", redirect.RouteValues["returnUrl"]);
        }
    }

    private static AuthenticationTestContext RequestWithTimestamp(string? timestamp)
    {
        var properties = new AuthenticationProperties();
        if (timestamp is not null)
        {
            properties.Items[TimestampKey] = timestamp;
        }
        return new AuthenticationTestContext(AuthenticationTestContext.Authenticated(IdentityConstants.ApplicationScheme, properties));
    }

    private static string Timestamp(int seconds) =>
        AuthenticationTestContext.Now.AddSeconds(seconds).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

    private sealed class TestPrincipalFactory : IUserClaimsPrincipalFactory<ApplicationUser>
    {
        public Task<ClaimsPrincipal> CreateAsync(ApplicationUser user) =>
            Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity(IdentityConstants.ApplicationScheme)));
    }
}
