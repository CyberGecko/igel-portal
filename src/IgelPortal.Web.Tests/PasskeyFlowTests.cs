using Altcha.AspNetCore;
using IgelPortal.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgelPortal.Web.Tests;

[TestClass]
public class PasskeyFlowTests
{
    [TestMethod]
    public async Task MissingTicketDoesNotClaimAChallenge()
    {
        using var request = new AuthenticationTestContext();
        var store = new ReplayStore();
        var flow = new PasskeyFlow(store, request.Http.RequestServices.GetRequiredService<TimeProvider>());

        Assert.IsFalse(await flow.TryConsumeAsync(request.Http));
        Assert.AreEqual(IdentityConstants.TwoFactorUserIdScheme, request.Authentication.RequestedScheme);
        Assert.AreEqual(0, store.Claims.Count);
    }

    [TestMethod]
    [DataRow(null, "Assertion", 60)]
    [DataRow("", "Assertion", 60)]
    [DataRow("challenge", null, 60)]
    [DataRow("challenge", "Attestation", 60)]
    [DataRow("challenge", "Assertion", null)]
    [DataRow("challenge", "Assertion", 0)]
    [DataRow("challenge", "Assertion", -1)]
    public async Task InvalidTicketDoesNotClaimAChallenge(string? state, string? operation, int? expiresInSeconds)
    {
        using var request = Ticket(state, operation, expiresInSeconds);
        var store = new ReplayStore();
        var flow = new PasskeyFlow(store, request.Http.RequestServices.GetRequiredService<TimeProvider>());

        Assert.IsFalse(await flow.TryConsumeAsync(request.Http));
        Assert.AreEqual(0, store.Claims.Count);
    }

    [TestMethod]
    [DataRow("Assertion", false)]
    [DataRow("Attestation", true)]
    public async Task ChallengeIsClaimedOnceAndRetainedPastTicketExpiry(string operation, bool registration)
    {
        using var request = Ticket("challenge", operation, 60);
        using var retry = Ticket("challenge", operation, 60);
        var store = new ReplayStore();
        var flow = new PasskeyFlow(store, request.Http.RequestServices.GetRequiredService<TimeProvider>());

        Assert.IsTrue(await flow.TryConsumeAsync(request.Http, registration));
        Assert.IsFalse(await flow.TryConsumeAsync(retry.Http, registration));
        Assert.AreEqual(AuthenticationTestContext.Now.AddMinutes(6), store.Claims["passkey:challenge"]);
    }

    private static AuthenticationTestContext Ticket(string? state, string? operation, int? expiresInSeconds)
    {
        var properties = new AuthenticationProperties
        {
            ExpiresUtc = expiresInSeconds is { } seconds ? AuthenticationTestContext.Now.AddSeconds(seconds) : null
        };
        if (state is not null)
        {
            properties.Items["PasskeyState"] = state;
        }
        if (operation is not null)
        {
            properties.Items["PasskeyOperation"] = operation;
        }
        return new AuthenticationTestContext(AuthenticationTestContext.Authenticated(IdentityConstants.TwoFactorUserIdScheme, properties));
    }

    private sealed class ReplayStore : IAltchaReplayStore
    {
        public Dictionary<string, DateTimeOffset> Claims { get; } = [];

        public ValueTask<bool> TryClaimAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Claims.TryAdd(key, expiresAt));
    }
}
