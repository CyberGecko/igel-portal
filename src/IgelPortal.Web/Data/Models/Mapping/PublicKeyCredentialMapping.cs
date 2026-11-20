using Fido2NetLib.Objects;
using Microsoft.Extensions.Localization;

namespace IgelPortal.Web.Data.Models.Mapping
{
    public class PublicKeyCredentialMapping(IStringLocalizer<PublicKeyCredentialMapping> localizer)
    {
        public PublicKeyCredential Map(Entities.PublicKeyCredential credential) => new()
        {
            Id = Convert.ToBase64String(credential.Id),
            UserId = credential.UserId,
            AuthenticationTransports = credential.AuthenticatorTransports.Select(value => ConvertToReadable(value.Value)).ToList(),
            DevicePublicKeys = credential.DevicePublicKeys.Select(value => Convert.ToBase64String(value.Value)).ToList()
        };

        private String ConvertToReadable(AuthenticatorTransport value)
        {
            return value switch
            {
                AuthenticatorTransport.Usb => localizer.GetString("USB Authenticator"),
                AuthenticatorTransport.Nfc => localizer.GetString("NFC Authenticator"),
                AuthenticatorTransport.Ble => localizer.GetString("Bluetooth Low Energy Authenticator"),
                AuthenticatorTransport.SmartCard => localizer.GetString("Smart Card Authenticator"),
                AuthenticatorTransport.Hybrid => localizer.GetString("Combined Data-Transport/Proximity Authenticator"),
                AuthenticatorTransport.Internal => localizer.GetString("Device Specific Authenticator"),
                _ => localizer.GetString("Unknown Authenticator '{0}'", value),
            };
        }
    }
}
