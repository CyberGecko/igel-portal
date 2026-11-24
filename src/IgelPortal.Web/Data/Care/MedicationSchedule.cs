using System.Globalization;

namespace IgelPortal.Web.Data.Care;

internal static class MedicationSchedule
{
    internal static bool TryBuild(CareClock clock, DateOnly today, string? from, string? to,
        string?[]? dailyTimes, out IReadOnlyList<DateTime> schedule) =>
        TryBuild(clock, today, from, to, dailyTimes, out schedule, out _);

    internal static bool TryBuild(CareClock clock, DateOnly today, string? from, string? to,
        string?[]? dailyTimes, out IReadOnlyList<DateTime> schedule, out string? invalidField)
    {
        schedule = [];
        invalidField = "startDate";
        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
            start < today)
            return false;

        invalidField = "endDate";
        if (!DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end) ||
            end.DayNumber - start.DayNumber is < 0 or >= 90)
            return false;

        invalidField = "dailyTimes";
        if (dailyTimes is null || dailyTimes.Length > 4)
            return false;

        var times = new SortedSet<TimeOnly>();
        foreach (var value in dailyTimes)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) || !times.Add(time))
                return false;
        }
        if (times.Count == 0) return false;

        var result = new List<DateTime>();
        for (int offset = 0; offset <= end.DayNumber - start.DayNumber; offset++)
        {
            foreach (var time in times)
            {
                if (!clock.TryToUtc(start.AddDays(offset).ToDateTime(time), out var day)) return false;
                result.Add(day);
            }
        }
        schedule = result;
        invalidField = null;
        return true;
    }
}
