namespace GarageGroup.Internal.Timesheet;

internal static class PeriodSetGetMetadata
{
    public static class Func
    {
        public const string Tag = "Period";

        public const string Route = "/getPeriods";

        public const string Summary
            =
            "Get active periods";

        public const string Description
            =
            "Retrieves all active periods";
    }

    public static class Out
    {
        public const string PeriodsDescription
            =
            "Array of periods items.";

        public const string NameDescription
            =
            "Name of the period";

        public const string NameExample = "December 2025";

        public const string DateFromDescription
            =
            "Start date of the period";

        public const string DateFromExample = "2025-12-01";

        public const string DateToDescription
            =
            "End date of the period";

        public const string DateToExample = "2025-12-31";
    }
}