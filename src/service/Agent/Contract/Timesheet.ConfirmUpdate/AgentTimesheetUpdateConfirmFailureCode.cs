namespace GarageGroup.Internal.Timesheet;

public enum AgentTimesheetUpdateConfirmFailureCode
{
    Unknown,
    InvalidActionId,
    NotFound,
    Expired,
    InvalidState,
    Conflict,
    BadRequest,
    ProjectNotFound,
    Indeterminate
}
