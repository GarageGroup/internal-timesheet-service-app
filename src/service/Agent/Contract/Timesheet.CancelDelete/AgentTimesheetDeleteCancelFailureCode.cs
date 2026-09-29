namespace GarageGroup.Internal.Timesheet;

public enum AgentTimesheetDeleteCancelFailureCode
{
    Unknown,
    InvalidActionId,
    NotFound,
    Expired,
    InvalidState,
    Conflict
}
