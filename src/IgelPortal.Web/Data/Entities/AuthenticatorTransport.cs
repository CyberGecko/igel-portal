namespace IgelPortal.Web.Data.Entities
{
    public class AuthenticatorTransport
    {
        public required Byte[] PublicKeyCredentialId { get; set; }

        public required Fido2NetLib.Objects.AuthenticatorTransport Value { get; set; }
    }
}
