using System;

namespace GarageGroup.Internal.Timesheet;

// The service does not know the time zone of the user, so the day is taken in the earliest time zone (UTC-12):
// a period is never closed for a user who still has the last open day
internal sealed class TodayProvider : ITodayProvider
{
    private static readonly TimeSpan EarliestTimeZoneOffset = TimeSpan.FromHours(-12);

    public static readonly TodayProvider Instance;

    static TodayProvider()
        =>
        Instance = new();

    private TodayProvider()
    {
    }

    public DateOnly EarliestToday
        =>
        DateOnly.FromDateTime(DateTime.UtcNow.Add(EarliestTimeZoneOffset));
}
