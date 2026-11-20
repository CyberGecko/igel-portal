using Fido2NetLib.Objects;

namespace IgelPortal.Web.Handler.Fido2
{
    public static partial class Fido2Handler
    {
        public class CreateAttestationOptionsInputModel
        {
            public string AttestationType { get; set; } = "none";

            public AuthenticatorAttachment? AuthenticatorAttachment { get; set; }

            public ResidentKeyRequirement ResidentKey { get; set; } = ResidentKeyRequirement.Discouraged;

            public UserVerificationRequirement UserVerification { get; set; } = UserVerificationRequirement.Preferred;
        }
    }
}
