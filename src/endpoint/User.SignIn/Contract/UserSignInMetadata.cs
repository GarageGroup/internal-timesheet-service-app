namespace GarageGroup.Internal.Timesheet;

internal static class UserSignInMetadata
{
    public static class Func
    {
        public const string Tag = "Authorization";

        public const string Route = "/signIn";

        public const string Summary
            =
            "Sign in";

        public const string Description
            =
            "Log in the user.";
    }

    public static class FailureCode
    {
        public const string SystemUserNotFoundMessage
            =
            "System user was not found";

        public const string InvalidTelegramDataMessage
            =
            "Telegram data is invalid";

        public const string TelegramUserAlreadyLinkedMessage
            =
            "Telegram user is already linked to another system user";
    }
}
