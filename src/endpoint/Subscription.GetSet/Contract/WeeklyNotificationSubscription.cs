using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace GarageGroup.Internal.Timesheet;

using static SubscriptionSetGetMetadata;

public sealed record class WeeklyNotificationSubscription : SubscriptionBase
{
    public static OpenApiSchema Schema { get; }
        =
        new()
        {
            Title = "Weekly notification subscription",
            Description = "Weekly notification subscription",
            Example = new JsonObject
            {
                [NamingPolicy.ConvertName(nameof(NotificationType))] = nameof(NotificationType.WeeklyNotification),
                [NamingPolicy.ConvertName(nameof(UserPreference))] = WeeklyNotificationUserPreference.Example
            }
        };

    public WeeklyNotificationSubscription(WeeklyNotificationUserPreference? userPreference)
        =>
        UserPreference = userPreference;

    public override NotificationType NotificationType { get; } = NotificationType.WeeklyNotification;

    public override WeeklyNotificationUserPreference? UserPreference { get; }
}