using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Notification.Subscribe.Test;

partial class NotificationApiSource
{
    public static TheoryData<NotificationSubscribeIn, NotificationSubscriptionJson> InputValidTestData
        =>
        new()
        {
            {
                new(
                    systemUserId: new("e841030b-abc6-441b-9605-d606da13ca5f"),
                    subscriptionData: new DailyNotificationSubscriptionData(
                        userPreference: default)),
                new()
                {
                    IsDisabled = false
                }
            },
            {
                new(
                    systemUserId: new("5dfd3ac2-7bcb-47dc-b2da-581e6039b692"),
                    subscriptionData: new DailyNotificationSubscriptionData(
                        userPreference: new(
                            workedHours: 1,
                            notificationTime: default))),
                new()
                {
                    IsDisabled = false,
                    UserPreferences = "{\"workedHours\":1,\"flowRuntime\":\"18:00\"}"
                }
            },
            {
                new(
                    systemUserId: new("5dfd3ac2-7bcb-47dc-b2da-581e6039b692"),
                    subscriptionData: new DailyNotificationSubscriptionData(
                        userPreference: new(
                            workedHours: 8,
                            notificationTime: NotificationTime.Msk20))),
                new()
                {
                    IsDisabled = false,
                    UserPreferences = "{\"workedHours\":8,\"flowRuntime\":\"20:00\"}"
                }
            },
            {
                new(
                    systemUserId: new("e2518da8-0f66-464f-b6cd-d22a9c476e6d"),
                    subscriptionData: new WeeklyNotificationSubscriptionData(
                        userPreference: default)),
                new()
                {
                    IsDisabled = false
                }
            },
            {
                new(
                    systemUserId: new("f302bf8b-22a3-4354-bfb9-35a80c61fcdc"),
                    subscriptionData: new WeeklyNotificationSubscriptionData(
                        userPreference: new(
                            weekday: [Weekday.Friday],
                            workedHours: 35,
                            notificationTime: default))),
                new()
                {
                    IsDisabled = false,
                    UserPreferences = "{\"weekday\":\"5\",\"workedHours\":35,\"flowRuntime\":\"18:00\"}"
                }
            },
            {
                new(
                    systemUserId: new("f302bf8b-22a3-4354-bfb9-35a80c61fcdc"),
                    subscriptionData: new WeeklyNotificationSubscriptionData(
                        userPreference: new(
                            weekday: [Weekday.Friday],
                            workedHours: 1,
                            notificationTime: NotificationTime.Msk19))),
                new()
                {
                    IsDisabled = false,
                    UserPreferences = "{\"weekday\":\"5\",\"workedHours\":1,\"flowRuntime\":\"19:00\"}"
                }
            },
            {
                new(
                    systemUserId: new("1d8f2faa-1aa4-4b6e-958f-a455343d9e72"),
                    subscriptionData: new WeeklyNotificationSubscriptionData(
                        userPreference: new(
                            weekday: [Weekday.Saturday, Weekday.Friday, Weekday.Sunday],
                            workedHours: 40,
                            notificationTime: NotificationTime.Msk21))),
                new()
                {
                    IsDisabled = false,
                    UserPreferences = "{\"weekday\":\"6,5,0\",\"workedHours\":40,\"flowRuntime\":\"21:00\"}"
                }
            }
        };
}