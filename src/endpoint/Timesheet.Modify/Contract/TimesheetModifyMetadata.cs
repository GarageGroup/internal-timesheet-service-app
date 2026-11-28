namespace GarageGroup.Internal.Timesheet;

internal static class TimesheetModifyMetadata
{
    public static class Func
    {
        public const string Tag = "Timesheet";

        public const string RouteCreate = "/createTimesheet";

        public const string SummaryCreate
            =
            "Create timesheet";

        public const string DescriptionCreate
            =
            "Creates a new timesheet entry. The date must not be in the future or in a closed period.";

        public const string RouteUpdate = "/updateTimesheet";

        public const string SummaryUpdate
            =
            "Update timesheet";

        public const string DescriptionUpdate
            =
            "Updates an existing timesheet entry. Neither its current date nor the new one may be in a closed period, " +
            "and the new date must not be in the future.";
    }

    public static class In
    {
        public const string TimesheetIdDescription
            =
            "Unique identifier of the timesheet in Dataverse.";

        public const string TimesheetIdExample = "256db568-f141-4305-b801-c94629723de0";

        public const string DateDescription
            =
            "Date of the timesheet entry.";

        public const string ProjectDescription
            =
            "Project, lead, opportunity or incident associated with the timesheet.";

        public const string ProjectIdDescription
            =
            "Unique identifier of the associated project in Dataverse.";

        public const string ProjectIdExample = "3dcaa2da-a9f7-4950-8161-2e4f47fa0c23";

        public const string ProjectTypeDescription
            =
            "Type of the project";

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
    }

    public static class FailureCode
    {
        public const string UnexpectedProjectTypeMessage
            =
            "An unexpected project type";

        public const string ForbiddenMessage
            =
            "This method is forbidden for your account";

        public const string ProjectNotFoundMessage
            =
            "Project was not found";
    
        public const string TimesheetNotFoundMessage
            =
            "Timesheet was not found";

        public const string EmptyDescriptionMessage
            =
            "Description must be specified";
    }
}
