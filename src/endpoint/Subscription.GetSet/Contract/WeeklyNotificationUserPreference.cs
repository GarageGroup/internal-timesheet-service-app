using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using static SubscriptionSetGetMetadata;

public sealed record class WeeklyNotificationUserPreference : INotificationUserPreference
{
    internal static JsonObject Example { get; }
        =
        new()
        {
            [NamingPolicy.ConvertName(nameof(WorkedHours))] = In.WeeklyNotificationWorkedHoursExample,
            [NamingPolicy.ConvertName(nameof(NotificationTime))] = In.NotificationTimeExample,
            [NamingPolicy.ConvertName(nameof(Weekday))] = new JsonArray
            {
                Timesheet.Weekday.Friday.ToString(),
                Timesheet.Weekday.Saturday.ToString(),
                Timesheet.Weekday.Sunday.ToString()
            }
        };

    public WeeklyNotificationUserPreference(
        FlatArray<Weekday> weekday,
        decimal workedHours,
        [AllowNull] string notificationTime)
    {
        Weekday = weekday;
        WorkedHours = workedHours;
        NotificationTime = notificationTime.OrEmpty();
    }

    [SwaggerDescription(In.WeeklyNotificationWeekdayDescription)]
    public FlatArray<Weekday> Weekday { get; }

    [SwaggerDescription(In.WeeklyNotificationWorkedHoursDescription)]
    public decimal WorkedHours { get; }

    [SwaggerDescription(In.NotificationTimeDescription)]
    public string NotificationTime { get; }
}