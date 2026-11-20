using Altcha.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace IgelPortal.Web.Services;

public sealed class PasskeyFlow(IAltchaReplayStore replayStore, TimeProvider clock)
{
    public const string RateLimitPolicy = "passkey";
    public const int CredentialSizeLimit = 32_768;

    public async Task<bool> TryConsumeAsync(HttpContext context, bool registration = false)
    {
        // SignInManager puts its opaque WebAuthn state into the protected temporary two-factor ticket.
        // Claim the state before verification because clearing the cookie cannot stop a concurrent request.
        var ticket = await context.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
        var operation = registration ? "Attestation" : "Assertion";
        if (!ticket.Succeeded || ticket.Properties is not { ExpiresUtc: { } expiresAt } properties ||
            expiresAt <= clock.GetUtcNow() || !properties.Items.TryGetValue("PasskeyOperation", out var actualOperation) ||
            actualOperation != operation || !properties.Items.TryGetValue("PasskeyState", out var state) ||
            string.IsNullOrEmpty(state))
        {
            return false;
        }

        // Retain the claim briefly after expiry for any requests already being processed.
        return await replayStore.TryClaimAsync("passkey:" + state, expiresAt.AddMinutes(5), context.RequestAborted);
    }
}
