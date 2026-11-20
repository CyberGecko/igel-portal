using System.Globalization;

namespace IgelPortal.Web.Data.Care;

public sealed class CareClock(IConfiguration configuration)
{
    private readonly TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(configuration["Care:TimeZone"] ?? "Europe/Berlin");

    public string ZoneId => zone.Id;
    public DateTime LocalNow => FromUtc(DateTime.UtcNow);
    public DateTime FromUtc(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);
    public string InputNow => LocalNow.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    public bool TryParseLocal(string? value, out DateTime utc)
    {
        utc = default;
        return DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)
            && TryToUtc(local, out utc);
    }

    public bool TryToUtc(DateTime local, out DateTime utc)
    {
        utc = default;
        if (zone.IsInvalidTime(local) || zone.IsAmbiguousTime(local))
        {
            return false;
        }
        utc = TimeZoneInfo.ConvertTimeToUtc(local, zone);
        return true;
    }
}
