using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed class DateProvider(TimeZoneInfo timeZone) : IDateProvider
{
    public DateOnly Today
        =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
}
