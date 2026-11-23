using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public enum AgentActionDecideFailureCode
{
    Unknown,

    [Problem(FailureStatusCode.BadRequest, "Telegram identity is invalid")]
    InvalidIdentity,

    [Problem(FailureStatusCode.NotFound, "Telegram user is not linked")]
    UserNotLinked,

    [Problem(FailureStatusCode.Forbidden, "Telegram user binding is unavailable")]
    UserUnavailable,

    [Problem(FailureStatusCode.Forbidden, "Agent write operations are disabled")]
    WriteDisabled,

    [Problem(FailureStatusCode.BadRequest, "Agent action decision is invalid")]
    InvalidDecision,

    [Problem(FailureStatusCode.NotFound, "Agent action was not found")]
    ActionNotFound,

    [Problem(FailureStatusCode.Conflict, "Agent action has expired")]
    ActionExpired,

    [Problem(FailureStatusCode.Conflict, "Agent action is not pending")]
    InvalidActionState,

    [Problem(FailureStatusCode.Conflict, "Agent action was changed by another request")]
    ActionConflict,

    [Problem(FailureStatusCode.BadRequest, "Timesheet data is invalid")]
    InvalidTimesheet,

    [Problem(FailureStatusCode.Forbidden, "Timesheet creation is forbidden")]
    TimesheetForbidden,

    [Problem(FailureStatusCode.NotFound, "Timesheet project was not found")]
    ProjectNotFound,

    [Problem(FailureStatusCode.Conflict, "Timesheet creation result is indeterminate")]
    Indeterminate
}
