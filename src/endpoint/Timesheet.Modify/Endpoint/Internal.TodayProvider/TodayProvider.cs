using System;

namespace GarageGroup.Internal.Timesheet;

// The service does not know the time zone of the user, so the day is taken in the latest time zone (UTC+14):
// a date that is today for any user is never rejected as a future one
internal sealed class TodayProvider : ITodayProvider
{
    private static readonly TimeSpan LatestTimeZoneOffset = TimeSpan.FromHours(14);

    public static readonly TodayProvider Instance;

    static TodayProvider()
        =>
        Instance = new();

    private TodayProvider()
    {
    }

    public DateOnly Today
        =>
        DateOnly.FromDateTime(DateTime.UtcNow.Add(LatestTimeZoneOffset));
}
