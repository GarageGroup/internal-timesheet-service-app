namespace GarageGroup.Internal.Timesheet;

public enum AgentTimesheetUpdateCancelFailureCode
{
    Unknown,
    InvalidActionId,
    NotFound,
    Expired,
    InvalidState,
    Conflict
}
