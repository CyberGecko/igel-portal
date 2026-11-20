using Fido2NetLib;
using Fido2NetLib.Objects;
using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Text;

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

            var options = AssertionOptions.FromJson(json);

            var credential = await fido2DataService.GetCredentialByAssertionResponseId(assertionResponse.Id, cancellationToken);

            if (credential is null)
            {
                return TypedResults.BadRequest();
            }

            (ApplicationUser? User, VerifyAssertionResult? AssertionResult)? UserAssertionResult = await fido2DataService.CreateAssertionForUser(credential, assertionResponse, options, cancellationToken);

            ApplicationUser? user = UserAssertionResult?.User;
            if (user is null)
            {
                return TypedResults.BadRequest();
            }

            await signInManager.SignInAsync(user, false);

            return TypedResults.Ok(UserAssertionResult?.AssertionResult);
        }

        public static async Task<Ok<AssertionOptions>> CreateAssertionOptions(HttpContext httpContext, [FromBody] CreateAssertionOptionsInputModel input,
            IFido2DataService fido2DataService, CancellationToken cancellationToken = default)
        {
            AssertionOptions? options = await fido2DataService.CreateAssertionOptions(input.UserName, input.UserVerification, cancellationToken);

            httpContext.Session.SetString("Fido2AssertionOptions", options?.ToJson() ?? String.Empty);

            return TypedResults.Ok(options);
        }

        public static async Task<Results<BadRequest, Ok<RegisteredPublicKeyCredential>>> CreateAttestation([FromBody] AuthenticatorAttestationRawResponse attestationResponse,
             HttpContext httpContext, IFido2DataService fido2DataService, CancellationToken cancellationToken = default)
        {
            var json = httpContext.Session.GetString("Fido2AttestationOptions");

            if (String.IsNullOrEmpty(json))
            {
                return TypedResults.BadRequest();
            }

            try
            {
                RegisteredPublicKeyCredential? registeredPublicKeyCredential = await fido2DataService.MakeNewCredentials(json, attestationResponse, cancellationToken);

                ApplicationUser? user = await fido2DataService.AddNewCredentialsToUser(registeredPublicKeyCredential, cancellationToken);

                if (user != null)
                {
                    return TypedResults.Ok(registeredPublicKeyCredential);
                }
                else
                {
                    throw new Exception("User not found");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static Ok<CredentialCreateOptions> CreateAttestationOptions([FromBody] CreateAttestationOptionsInputModel input, HttpContext httpContext, IFido2DataService fido2DataService)
        {
            CredentialCreateOptions? options = fido2DataService.CreateAttestationOptions(input);

            httpContext.Session.SetString("Fido2AttestationOptions", options?.ToJson() ?? String.Empty);

            return TypedResults.Ok(options);
        }
    }
}
