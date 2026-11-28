namespace GarageGroup.Internal.Timesheet;

internal static class ProjectSetGetMetadata
{
    public static class Func
    {
        public const string Tag = "Project";

        public const string Route = "/getProjects";

        public const string Summary
            =
            "Get projects, leads, opportunities and incidents";

        public const string Description
            =
            "Retrieves projects, leads, opportunities, and incidents: the ones the user has logged time to recently come first. " +
            "All of them are returned unless the 'top' parameter limits the number.";
    }

    public static class In
    {
        public const string TopDescription
            =
            "Maximum number of items to return. All items are returned if it is not specified.";

        public const int TopExample = 20;
    }

    public static class Out
    {
        public const string ProjectsDescription
            =
            "Array of project items.";

        public const string IdDescription
            =
            "Unique identifier in Dataverse.";

        public const string IdExample = "4bb0ff0d-3ac1-4408-8657-2bc56ed9bf42";

        public const string NameDescription
            =
            "Name of the project, lead, opportunity or incident.";

        public const string NameExample = "Internal Timesheet";

        public const string TypeDescription
            =
            "Type of the project";

        public const string TypeExample = nameof(ProjectType.Project);

        public const string CommentDescription
            =
            "Description of the project.";

        public const string CommentExample = "All internal activities.";
    }
}