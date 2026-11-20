using System.Text;
using System.Text.Json;
using Altcha;
using Altcha.AspNetCore;
using IgelPortal.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace IgelPortal.Web.Tests;

[TestClass]
public class RegistrationCaptchaTests
{
    private const string SessionId = "registration-session";

    [TestMethod]
    public void ChallengeReusesTheNonceWithinOneSession()
    {
        var request = RequestWithForm(sessionId: null);
        var first = RegistrationCaptcha.CreateChallengeData(request)["registrationSession"];
        var second = RegistrationCaptcha.CreateChallengeData(request)["registrationSession"];
        var other = RegistrationCaptcha.CreateChallengeData(RequestWithForm(sessionId: null))["registrationSession"];

        Assert.AreEqual(first, second);
        Assert.AreNotEqual(first, other);
    }

    [TestMethod]
    [DataRow(null, "payload")]
    [DataRow(SessionId, null)]
    [DataRow(SessionId, "")]
    public async Task MissingSessionOrPayloadIsRejectedWithoutCallingAltcha(string? sessionId, string? payload)
    {
        var altcha = Substitute.For<IAltchaService>();
        var request = RequestWithForm(sessionId, payload);

        Assert.IsFalse(await new RegistrationCaptcha(altcha).VerifyAsync(request));
        await altcha.DidNotReceiveWithAnyArgs().VerifyAsync(default!);
    }

    [TestMethod]
    public async Task RepeatedOrOverlongFormValuesAreRejectedWithoutCallingAltcha()
    {
        var altcha = Substitute.For<IAltchaService>();
        var captcha = new RegistrationCaptcha(altcha);

        Assert.IsFalse(await captcha.VerifyAsync(RequestWithForm(SessionId, new[] { "first", "second" })));
        Assert.IsFalse(await captcha.VerifyAsync(RequestWithForm(SessionId, new string('a', 8193))));
        await altcha.DidNotReceiveWithAnyArgs().VerifyAsync(default!);
    }

    [TestMethod]
    [DataRow(true, SessionId, true)]
    [DataRow(true, "different-session", false)]
    [DataRow(false, SessionId, false)]
    public async Task VerificationRequiresAValidChallengeFromTheSameSession(bool verified, string challengeSession, bool accepted)
    {
        var altcha = Substitute.For<IAltchaService>();
        var challenge = new Challenge
        {
            Parameters = new ChallengeParameters
            {
                Algorithm = "SHA-256", Nonce = "00", Salt = "00", KeyPrefix = "00",
                Data = new Dictionary<string, JsonElement>
                {
                    ["registrationSession"] = JsonSerializer.SerializeToElement(challengeSession)
                }
            },
            Signature = "test"
        };
        altcha.VerifyAsync("payload").Returns(new AltchaVerificationResult
        {
            Verified = verified,
            Payload = new Payload { Challenge = challenge, Solution = new() }
        });

        Assert.AreEqual(accepted, await new RegistrationCaptcha(altcha).VerifyAsync(RequestWithForm(SessionId, "payload")));
    }

    private static DefaultHttpContext RequestWithForm(string? sessionId, StringValues payload = default)
    {
        var session = Substitute.For<ISession>();
        var bytes = sessionId is null ? null : Encoding.UTF8.GetBytes(sessionId);
        session.TryGetValue(Arg.Any<string>(), out Arg.Any<byte[]?>()).Returns(call =>
        {
            call[1] = bytes;
            return bytes is not null;
        });
        session.When(store => store.Set(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(call => bytes = call.Arg<byte[]>());
        var context = new DefaultHttpContext { Session = session };
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["Altcha"] = payload });
        return context;
    }
}
