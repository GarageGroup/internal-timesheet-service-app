using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using static SubscriptionSetGetMetadata;

public sealed record class DailyNotificationUserPreference : INotificationUserPreference
{
    internal static JsonObject Example { get; }
        =
        new()
        {
            [NamingPolicy.ConvertName(nameof(WorkedHours))] = In.DailyNotificationWorkedHoursExample,
            [NamingPolicy.ConvertName(nameof(NotificationTime))] = In.NotificationTimeExample
        };

    public DailyNotificationUserPreference(decimal workedHours, [AllowNull] string notificationTime)
    {
        WorkedHours = workedHours;
        NotificationTime = notificationTime.OrEmpty();
    }

    [SwaggerDescription(In.DailyNotificationWorkedHoursDescription)]
    public decimal WorkedHours { get; }

    [SwaggerDescription(In.NotificationTimeDescription)]
    public string NotificationTime { get; }
}