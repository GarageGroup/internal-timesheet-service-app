namespace GarageGroup.Internal.Timesheet;

public enum AgentTimesheetCreatePrepareFailureCode
{
    Unknown,
    InvalidProject,
    InvalidDuration,
    EmptyDescription,
    Conflict,
    Forbidden
}
