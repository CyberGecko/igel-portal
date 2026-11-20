using System.Globalization;
using System.Security.Claims;
using IgelPortal.Web.Data.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IgelPortal.Web.Services;

public sealed class PortalSignInManager(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> options,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(userManager, contextAccessor, claimsFactory, options, logger, schemes, confirmation)
{
    private const string AuthenticatedAt = "igel.authenticated-at";
    private static readonly TimeSpan ConfirmationLifetime = TimeSpan.FromMinutes(5);

    public override Task SignInWithClaimsAsync(ApplicationUser user, AuthenticationProperties? authenticationProperties,
        IEnumerable<Claim> additionalClaims)
    {
        authenticationProperties ??= new AuthenticationProperties();
        // RefreshSignInAsync passes the existing ticket. A refresh must never count as a new authentication.
        if (authenticationProperties.IssuedUtc is null)
        {
            authenticationProperties.Items[AuthenticatedAt] =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        }

        return base.SignInWithClaimsAsync(user, authenticationProperties, additionalClaims);
    }

    public static async Task<bool> HasRecentAuthenticationAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var now = DateTimeOffset.UtcNow;
        return result.Succeeded && result.Properties is { } properties &&
            properties.Items.TryGetValue(AuthenticatedAt, out var value) &&
            long.TryParse(value, CultureInfo.InvariantCulture, out var timestamp) &&
            timestamp <= now.ToUnixTimeSeconds() &&
            timestamp >= (now - ConfirmationLifetime).ToUnixTimeSeconds();
    }
}
