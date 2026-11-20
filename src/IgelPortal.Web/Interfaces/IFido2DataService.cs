using Fido2NetLib;
using Fido2NetLib.Objects;
using Entities = IgelPortal.Web.Data.Entities;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Handler.Fido2;
using System.Security.Principal;

namespace IgelPortal.Web.Interfaces
{
    public interface IFido2DataService
    {
        Task<ApplicationUser?> AddNewCredentialsToUser(RegisteredPublicKeyCredential? registeredPublicKeyCredential, CancellationToken cancellationToken = default);
        Task<AssertionOptions?> CreateAssertionOptions(String userName, UserVerificationRequirement userVerification, CancellationToken cancellationToken = default);
        CredentialCreateOptions? CreateAttestationOptions(Fido2Handler.CreateAttestationOptionsInputModel input);
        Task<(ApplicationUser? User, VerifyAssertionResult? AssertionResult)?> CreateAssertionForUser(Entities.PublicKeyCredential credential, AuthenticatorAssertionRawResponse assertionResponse, AssertionOptions options, CancellationToken cancellationToken = default);
        Task<Entities.PublicKeyCredential?> GetCredentialByAssertionResponseId(Byte[] assertionResponseId, CancellationToken cancellationToken = default);
        Task<RegisteredPublicKeyCredential?> MakeNewCredentials(String json, AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken = default);
        Task<Boolean> UserHasFidoCredentials(IIdentity identity);
        Task<IList<PublicKeyCredential>> GetUserFidoCredentials(IIdentity? identity);
        Task RemoveAuthentication(IIdentity identity);
    }
}