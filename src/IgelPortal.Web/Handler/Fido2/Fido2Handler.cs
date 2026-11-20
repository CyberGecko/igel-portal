using Fido2NetLib;
using Fido2NetLib.Objects;
using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;

namespace IgelPortal.Web.Handler.Fido2
{
    public static partial class Fido2Handler
    {
        public static async Task<Results<BadRequest, Ok<VerifyAssertionResult>>> CreateAssertion([FromBody] AuthenticatorAssertionRawResponse assertionResponse, HttpContext httpContext,
            SignInManager<ApplicationUser> signInManager, IFido2DataService fido2DataService, CancellationToken cancellationToken = default)
        {
            var json = httpContext.Session.GetString("Fido2AssertionOptions");

            if (string.IsNullOrEmpty(json))
            {
                return TypedResults.BadRequest();
            }

            httpContext.Session.Remove("Fido2AssertionOptions");
            var options = AssertionOptions.FromJson(json);

            var credential = await fido2DataService.GetCredentialByAssertionResponseId(assertionResponse.Id, cancellationToken);

            if (credential is null)
            {
                return TypedResults.BadRequest();
            }

            try
            {
                var result = await fido2DataService.CreateAssertionForUser(credential, assertionResponse, options, cancellationToken);
                if (result?.User is not { } user ||
                    !await signInManager.CanSignInAsync(user) ||
                    (signInManager.UserManager.SupportsUserLockout && await signInManager.UserManager.IsLockedOutAsync(user)))
                {
                    return TypedResults.BadRequest();
                }

                await signInManager.SignInAsync(user, false, "Passkey");
                return TypedResults.Ok(result.Value.AssertionResult);
            }
            catch (Fido2VerificationException)
            {
                return TypedResults.BadRequest();
            }
        }

        public static async Task<Ok<AssertionOptions>> CreateAssertionOptions(HttpContext httpContext, [FromBody] CreateAssertionOptionsInputModel input,
            IFido2DataService fido2DataService, CancellationToken cancellationToken = default)
        {
            AssertionOptions? options = await fido2DataService.CreateAssertionOptions(input.UserName, input.UserVerification, cancellationToken);

            httpContext.Session.SetString("Fido2AssertionOptions", options?.ToJson() ?? String.Empty);

            return TypedResults.Ok(options);
        }

        public static async Task<IResult> CreateAttestation([FromBody] AuthenticatorAttestationRawResponse response,
            HttpContext context, UserManager<ApplicationUser> users, IAntiforgery antiforgery,
            IFido2DataService fido2, CancellationToken cancellationToken)
        {
            if (!await antiforgery.IsRequestValidAsync(context))
            {
                return TypedResults.BadRequest();
            }

            var user = await users.GetUserAsync(context.User);
            var json = context.Session.GetString("Fido2AttestationOptions");
            context.Session.Remove("Fido2AttestationOptions");

            if (user is null || string.IsNullOrEmpty(json) ||
                !Guid.TryParse(user.Id, out var userId) ||
                !CredentialCreateOptions.FromJson(json).User.Id.SequenceEqual(userId.ToByteArray()))
            {
                return TypedResults.BadRequest();
            }

            try
            {
                var credential = await fido2.MakeNewCredentials(json, response, cancellationToken);
                return await fido2.AddNewCredentialsToUser(credential, user, cancellationToken) is null
                    ? TypedResults.BadRequest()
                    : TypedResults.Ok();
            }
            catch (Fido2VerificationException)
            {
                return TypedResults.BadRequest();
            }
        }

        public static async Task<IResult> CreateAttestationOptions([FromBody] CreateAttestationOptionsInputModel input,
            HttpContext context, UserManager<ApplicationUser> users, IAntiforgery antiforgery, IFido2DataService fido2)
        {
            if (!await antiforgery.IsRequestValidAsync(context))
            {
                return TypedResults.BadRequest();
            }

            var user = await users.GetUserAsync(context.User);
            if (user is null || !Guid.TryParse(user.Id, out _))
            {
                return TypedResults.BadRequest();
            }

            var options = fido2.CreateAttestationOptions(input, user);
            context.Session.SetString("Fido2AttestationOptions", options?.ToJson() ?? string.Empty);
            return TypedResults.Ok(options);
        }
    }
}
