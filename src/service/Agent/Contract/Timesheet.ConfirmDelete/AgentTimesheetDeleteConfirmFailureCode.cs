namespace GarageGroup.Internal.Timesheet;

public enum AgentTimesheetDeleteConfirmFailureCode
{
    Unknown,
    InvalidActionId,
    NotFound,
    Expired,
    InvalidState,
    Conflict,
    BadRequest,
    Indeterminate
}
