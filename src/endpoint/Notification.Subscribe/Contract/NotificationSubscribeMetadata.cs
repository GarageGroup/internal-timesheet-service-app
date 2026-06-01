namespace GarageGroup.Internal.Timesheet;

internal static class NotificationSubscribeMetadata
{
    public static class Func
    {
        public const string Tag = "Notification";

        public const string NameSubscribe = "SubscribeToNotifications";

        public const string RouteSubscribe = "/subscribeToNotification";

        public const string SummarySubscribe
            =
            "Subscribe bot user to notification";

        public const string DescriptionSubscribe
            =
            "Allows a bot user to subscribe to specific notifications";

        public const string NameUnsubscribe = "UnsubscribeFromNotifications";

        public const string RouteUnsubscribe = "/unsubscribeFromNotification";

        public const string SummaryUnsubscribe
            =
            "Unsubscribe bot user from notification";

        public const string DescriptionUnsubscribe
            =
            "Allows a bot user to stop receiving specific notifications";
    }

    public static class In
    {
        public const string NotificationTypeDesciption
            =
            "Type of notifications for the bot user";

        public const string NotificationTimeDescription
            =
            "Preferred notification time in the Moscow timezone (UTC+3)";

        public const string DailyNotificationUserPreferenceDesciption
            =
            "User's daily notification preferences";

        public const string DailyNotificationWorkedHoursDescription
            =
            "Number of hours worked in a day";

        public const int DailyNotificationWorkedHoursExample = 8;

        public const string WeeklyNotificationUserPreferenceDesciption
            =
            "User's weekly notification preferences";

        public const string WeeklyNotificationWorkedHoursDescription
            =
            "Number of hours worked in a week";

        public const int WeeklyNotificationWorkedHoursExample = 40;

        public const string WeeklyNotificationWeekdayDescription
            =
            "Day or days when notifications are sent";

        public const string WeeklyNotificationWeekdayExample = nameof(Weekday.Friday);
    }

    public static class FailureCode
    {
        public const string NotificationTypeInvalidMessage
            =
            "Notification type is unknown";

        public const string BotUserNotFoundMessage
            =
            "Bot user was not found";

        public const string NotificationTypeNotFoundMessage
            =
            "Notification type was not found";
    }
}