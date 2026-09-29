namespace GarageGroup.Internal.Timesheet;

public enum AgentTimesheetCreateConfirmFailureCode
{
    Unknown,
    InvalidActionId,
    NotFound,
    Expired,
    InvalidState,
    Conflict,
    BadRequest,
    Forbidden,
    ProjectNotFound,
    Indeterminate
}
