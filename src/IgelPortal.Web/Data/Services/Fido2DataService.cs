using AutoMapper;
using Fido2NetLib;
using Fido2NetLib.Objects;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Handler.Fido2;
using IgelPortal.Web.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;
using AuthenticatorTransport = IgelPortal.Web.Data.Entities.AuthenticatorTransport;

namespace IgelPortal.Web.Data.Services
{
    public class Fido2DataService(IFido2 fido2, UserManager<ApplicationUser> userManager, ApplicationDbContext applicationDbContext, IMapper mapper) : IFido2DataService
    {
        public async Task<ApplicationUser?> AddNewCredentialsToUser(RegisteredPublicKeyCredential? registeredPublicKeyCredential, CancellationToken cancellationToken = default)
        {
            if (registeredPublicKeyCredential == null)
            {
                return null;
            }
            var credential = new Entities.PublicKeyCredential
            {
                Id = registeredPublicKeyCredential.Id,
                PublicKey = registeredPublicKeyCredential.PublicKey,
                SignatureCounter = registeredPublicKeyCredential.SignCount,
                IsBackupEligible = registeredPublicKeyCredential.IsBackupEligible,
                IsBackedUp = registeredPublicKeyCredential.IsBackedUp,
                AttestationObject = registeredPublicKeyCredential.AttestationObject,
                AttestationClientDataJson = registeredPublicKeyCredential.AttestationClientDataJson,
                AttestationFormat = registeredPublicKeyCredential.AttestationFormat,
                AaGuid = registeredPublicKeyCredential.AaGuid,
                UserId = new Guid(registeredPublicKeyCredential.User.Id).ToString()
            };

            foreach (var authenticatorTransport in registeredPublicKeyCredential.Transports)
            {
                credential.AuthenticatorTransports.Add(new AuthenticatorTransport
                {
                    PublicKeyCredentialId = registeredPublicKeyCredential.Id,
                    Value = authenticatorTransport
                });
            }

            if (registeredPublicKeyCredential.DevicePublicKey is not null)
            {
                credential.DevicePublicKeys.Add(new Entities.DevicePublicKey
                {
                    PublicKeyCredentialId = registeredPublicKeyCredential.Id,
                    Value = registeredPublicKeyCredential.DevicePublicKey
                });
            }

            var user = await userManager.FindByEmailAsync(registeredPublicKeyCredential.User.Name);
            if (user == null)
            {
                return null;
            }
            else
            {
                user.PublicKeyCredentials.Add(credential);

                var identityResult = await userManager.UpdateAsync(user);

                if (!identityResult.Succeeded)
                {
                    throw new Exception(identityResult.ToString());
                }

                return user;
            }
        }

        public async Task<AssertionOptions?> CreateAssertionOptions(String userName, UserVerificationRequirement userVerification, CancellationToken cancellationToken = default)
        {
            var normalizedUserName = userManager.NormalizeName(userName);

            var allowedCredentials = await userManager.Users
                .Where(user => user.NormalizedUserName == normalizedUserName)
                .Include(c => c.PublicKeyCredentials)
                .SelectMany(user => user.PublicKeyCredentials)
                .Select(credential => new PublicKeyCredentialDescriptor(credential.Id))
                .ToListAsync(cancellationToken);

            var extensions = new AuthenticationExtensionsClientInputs
            {
                Extensions = true,
                UserVerificationMethod = true,
                DevicePubKey = new AuthenticationExtensionsDevicePublicKeyInputs()
            };

            return fido2.GetAssertionOptions(allowedCredentials, userVerification, extensions);
        }

        public CredentialCreateOptions? CreateAttestationOptions(Fido2Handler.CreateAttestationOptionsInputModel input)
        {
            var user = new Fido2User
            {
                Name = input.UserName,
                Id = Guid.NewGuid().ToByteArray(),
                DisplayName = input.UserName
            };

            var authenticatorSelection = new AuthenticatorSelection
            {
                AuthenticatorAttachment = input.AuthenticatorAttachment,
                ResidentKey = input.ResidentKey,
                UserVerification = input.UserVerification
            };

            var attestationPreference = input.AttestationType.ToEnum<AttestationConveyancePreference>();

            var extensions = new AuthenticationExtensionsClientInputs
            {
                Extensions = true,
                UserVerificationMethod = true,
                DevicePubKey = new AuthenticationExtensionsDevicePublicKeyInputs { Attestation = input.AttestationType },
                CredProps = true
            };

            return fido2.RequestNewCredential(user, [], authenticatorSelection, attestationPreference, extensions);
        }

        public async Task<(ApplicationUser? User, VerifyAssertionResult? AssertionResult)?> CreateAssertionForUser(Entities.PublicKeyCredential credential,
            AuthenticatorAssertionRawResponse assertionResponse, AssertionOptions options, CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(credential.UserId);

            if (user is null)
            {
                return null;
            }

            var assertionResult = await fido2.MakeAssertionAsync(assertionResponse, options, credential.PublicKey, credential.DevicePublicKeys.Select(key => key.Value).ToList(),
                credential.SignatureCounter,
                async (@params, cancellationToken) =>
                    await userManager.Users
                        .Where(user => user.Id == new Guid(@params.UserHandle).ToString())
                        .SelectMany(user => user.PublicKeyCredentials)
                        .AnyAsync(credential => credential.Id == @params.CredentialId, cancellationToken),
            cancellationToken);

            credential.SignatureCounter = assertionResult.SignCount;

            if (assertionResult.DevicePublicKey is not null)
            {
                credential.DevicePublicKeys.Add(new Entities.DevicePublicKey
                {
                    PublicKeyCredentialId = assertionResult.CredentialId,
                    Value = assertionResult.DevicePublicKey
                });
            }

            applicationDbContext.PublicKeyCredentials.Update(credential);

            await applicationDbContext.SaveChangesAsync(cancellationToken);

            return (User: user, AssertionResult: assertionResult);
        }

        public async Task<Entities.PublicKeyCredential?> GetCredentialByAssertionResponseId(Byte[] assertionResponseId, CancellationToken cancellationToken = default)
        {
            return await userManager.Users
                                    .SelectMany(user => user.PublicKeyCredentials)
                                    .Include(credential => credential.DevicePublicKeys)
                                    .SingleOrDefaultAsync(credential => credential.Id == assertionResponseId, cancellationToken);
        }

        public async Task<RegisteredPublicKeyCredential?> MakeNewCredentials(string json, AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken = default)
        {
            var options = CredentialCreateOptions.FromJson(json);

            var credentialResult = await fido2.MakeNewCredentialAsync(attestationResponse, options, async (@params, cancellationToken) =>
                                                                        await userManager.Users.SelectMany(user => user.PublicKeyCredentials)
                                                                                               .AllAsync(credential => credential.Id != @params.CredentialId, cancellationToken),
                                                                      cancellationToken);


            if (credentialResult.Result is null)
            {
                throw new Exception(credentialResult.ErrorMessage);
            }
            return credentialResult.Result;
        }

        public async Task<Boolean> UserHasFidoCredentials(IIdentity? identity)
        {
            if (identity?.Name == null)
            {
                return false;
            }
            var user = await userManager.FindByNameAsync(identity.Name);
            if (user == null)
            {
                return false;
            }
            return await userManager.Users.Where(u => u.Id == user.Id)
                                          .Include(u => u.PublicKeyCredentials)
                                          .SelectMany(u => u.PublicKeyCredentials)
                                          .Include(c => c.DevicePublicKeys)
                                          .AnyAsync();
        }
        public async Task<IList<PublicKeyCredential>> GetUserFidoCredentials(IIdentity? identity)
        {
            if (identity?.Name == null)
            {
                return [];
            }
            var user = await userManager.FindByNameAsync(identity.Name);
            if (user == null)
            {
                return [];
            }
            var entities = await userManager.Users.Where(u => u.Id == user.Id)
                                          .Include(u => u.PublicKeyCredentials)
                                          .SelectMany(u => u.PublicKeyCredentials)
                                          .Include(c => c.DevicePublicKeys)
                                          .Include(c => c.AuthenticatorTransports)
                                          .ToListAsync();
            return mapper.Map<IList<PublicKeyCredential>>(entities);

        }

        public async Task RemoveAuthentication(IIdentity identity)
        {
            if (identity?.Name == null)
            {
                return;
            }
            var normalizedUserName = userManager.NormalizeName(identity.Name);

            var user = await userManager.Users.Where(u => u.NormalizedUserName == normalizedUserName)
                                          .Include(u => u.PublicKeyCredentials)
                                          .ThenInclude(c => c.DevicePublicKeys)
                                          .Include(e => e.PublicKeyCredentials)
                                          .ThenInclude(e => e.AuthenticatorTransports)
                                          .FirstOrDefaultAsync();
            if (user == null)
            {
                return;
            }
            user.PublicKeyCredentials.Clear();
            await userManager.UpdateAsync(user);
        }
    }
}
