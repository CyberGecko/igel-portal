using Fido2NetLib.Objects;

namespace IgelPortal.Web.Handler.Fido2
{
    public static partial class Fido2Handler
    {
        public class CreateAssertionOptionsInputModel
        {
            public required string UserName { get; set; }

            public UserVerificationRequirement UserVerification { get; set; } = UserVerificationRequirement.Preferred;
        }
    }

}
