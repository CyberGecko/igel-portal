using AutoMapper;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Localization;

namespace IgelPortal.Web.Data.Models.Mapping
{
    public class PublicKeyCredentialMapping : Profile
    {
        private readonly IStringLocalizer<PublicKeyCredentialMapping> localizer;
        public PublicKeyCredentialMapping(IStringLocalizer<PublicKeyCredentialMapping> localizer)
        {
            this.localizer = localizer;

            CreateMap<Entities.PublicKeyCredential, PublicKeyCredential>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Convert.ToBase64String(src.Id)))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.AuthenticationTransports, opt => opt.MapFrom(src => src.AuthenticatorTransports.Select(value => ConvertToReadable(value.Value))))
                .ForMember(dest => dest.DevicePublicKeys, opt => opt.MapFrom(src => src.DevicePublicKeys.Select(value => Convert.ToBase64String(value.Value))));
        }

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
