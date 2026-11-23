namespace GarageGroup.Internal.Timesheet;

public enum AgentTimesheetUpdatePrepareFailureCode
{
    Unknown,
    InvalidTimesheet,
    InvalidDuration,
    EmptyChanges,
    NotFound,
    ReadOnly,
    InvalidProject,
    Conflict
}
