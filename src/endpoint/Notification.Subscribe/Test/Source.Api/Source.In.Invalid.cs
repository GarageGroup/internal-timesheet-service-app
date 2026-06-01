using System;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Notification.Subscribe.Test;

partial class NotificationApiSource
{
    public static TheoryData<NotificationSubscribeIn, Failure<NotificationSubscribeFailureCode>> InputInvalidTestData
        =>
        new()
        {
            {
                new(
                    systemUserId: new("dc028fce-4d9d-4661-be73-fbf0f3a07cf4"),
                    subscriptionData: new DailyNotificationSubscriptionData(
                        userPreference: new(
                            workedHours: 0,
                            notificationTime: NotificationTime.Msk18))),
                new(NotificationSubscribeFailureCode.InvalidQuery, "Daily working hours cannot be less than zero")
            },
            {
                new(
                    systemUserId: new("94e99792-5c51-419c-8443-0934b87958d3"),
                    subscriptionData: new DailyNotificationSubscriptionData(
                        userPreference: new(
                            workedHours: -10,
                            notificationTime: NotificationTime.Msk19))),
                new(NotificationSubscribeFailureCode.InvalidQuery, "Daily working hours cannot be less than zero")
            },
            {
                new(
                    systemUserId: new("9b572903-1933-4dbb-901e-023677462b56"),
                    subscriptionData: new WeeklyNotificationSubscriptionData(
                        userPreference: new(
                            weekday: [Weekday.Friday],
                            workedHours: 0,
                            notificationTime: NotificationTime.Msk20))),
                new(NotificationSubscribeFailureCode.InvalidQuery, "Total week working hours cannot be less than zero")
            },
            {
                new(
                    systemUserId: new("2fa0b63c-2403-4eff-8a43-9647a2ccd96a"),
                    subscriptionData: new WeeklyNotificationSubscriptionData(
                        userPreference: new(
                            weekday: [Weekday.Friday, Weekday.Saturday],
                            workedHours: -10,
                            notificationTime: NotificationTime.Msk21))),
                new(NotificationSubscribeFailureCode.InvalidQuery, "Total week working hours cannot be less than zero")
            },
            {
                new(
                    systemUserId: new("266e25be-1861-46f8-9e30-b1b250b277f9"),
                    subscriptionData: new WeeklyNotificationSubscriptionData(
                        userPreference: new(
                            weekday: default,
                            workedHours: 40,
                            notificationTime: NotificationTime.Msk21))),
                new(NotificationSubscribeFailureCode.InvalidQuery, "Weekdays for notifications must be specified")
            }
        };
}