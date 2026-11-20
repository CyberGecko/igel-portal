using Altcha;
using Altcha.AspNetCore;
using System.Security.Cryptography;
using System.Text.Json;

namespace IgelPortal.Web.Services;

public sealed class RegistrationCaptcha(IAltchaService altcha)
{
    public const string ChallengeRateLimit = "registration-captcha-challenge";
    public const string SubmitRateLimit = "registration-captcha-submit";
    public const string ErrorMessage = "Complete the security check again before registering.";

    private const string SessionKey = "RegistrationCaptcha";
    private const string SessionClaim = "registrationSession";

    public static Dictionary<string, object?> CreateChallengeData(HttpContext? context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var session = context.Session.GetString(SessionKey);
        if (session is null)
        {
            session = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            context.Session.SetString(SessionKey, session);
        }

        return new Dictionary<string, object?> { [SessionClaim] = session };
    }

    public IResult IssueChallenge(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Json(altcha.CreateChallenge(context), AltchaJson.SerializerOptions);
    }

    public async Task<bool> VerifyAsync(HttpContext context)
    {
        var session = context.Session.GetString(SessionKey);
        if (session is null || !context.Request.HasFormContentType)
        {
            return false;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        if (!form.TryGetValue("Altcha", out var values) ||
            values.Count != 1 || values[0] is not { Length: > 0 and <= 8192 } payload)
        {
            return false;
        }

        var result = await altcha.VerifyAsync(payload, cancellationToken: context.RequestAborted);
        return result.Verified &&
            result.Payload?.Challenge.Parameters.Data is { } data &&
            data.TryGetValue(SessionClaim, out var claim) &&
            claim.ValueKind == JsonValueKind.String &&
            claim.GetString() == session;
    }
}
