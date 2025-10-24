namespace GarageGroup.Internal.Timesheet;

internal static class TimesheetSetGetMetadata
{
    public static class Func
    {
        public const string Tag = "Timesheet";

        public const string Route = "/getTimesheets";

        public const string Summary
            =
            "Get timesheets";

        public const string Description
            =
            "Retrieves a list of timesheets.";
    }

    public static class In
    {
        public const string DateFromDescription
            =
            "The minimum date for the date range.";

        public const string DateToDescription
            =
            "The maximum date for the date range.";
    }

    public static class Out
    {
        public const string TimesheetsDescription
            =
            "Array of timesheet items.";

        public const string IdDescription
            =
            "Unique identifier of the timesheet in Dataverse.";

        public const string IdExample = "27a82cb6-ee49-45a1-a623-75f9304ed000";

        public const string ProjectIdDescription
            =
            "Unique identifier of the associated project in Dataverse.";

        public const string ProjectIdExample = "36ab76b3-48c1-4470-9344-ed3ed71f4e1f";

        public const string ProjectNameDescription
            =
            "Name of the associated project, lead, opportunity or incident.";

        public const string ProjectNameExample = "Internal Timesheet";

        public const string ProjectTypeDescription
            =
            "Type of the project";

        public const string ProjectCommentDescription
            =
            "Description of the project.";

        public const string ProjectCommentExample = "All internal activities.";

        public const string ProjectTypeExample = nameof(ProjectType.Project);

        public const string DurationDescription
            =
            "Duration logged in the timesheet.";

        public const int DurationExample = 2;

        public const string DescriptionDescription
            =
            "Description of the timesheet entry.";

        public const string DescriptionExample
            =
            "#Task8137_NewApiMethod. Implement new API method";

        public const string TimesheetIsActiveDescription
            =
            "Flag indicating whether the timesheet is active.";

        public const string DateDescription
            =
            "Date of the timesheet entry.";
    }
}