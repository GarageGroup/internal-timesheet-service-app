using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetModifyMetadata;

public enum TimesheetUpdateFailureCode
{
    Unknown,

    [Problem(FailureStatusCode.BadRequest, true)]
    BadRequest,

    [Problem(FailureStatusCode.BadRequest, FailureCode.UnexpectedProjectTypeMessage)]
    UnexpectedProjectType,

    [Problem(FailureStatusCode.NotFound, FailureCode.TimesheetNotFoundMessage)]
    TimesheetNotFound,

    [Problem(FailureStatusCode.NotFound, FailureCode.ProjectNotFoundMessage)]
    ProjectNotFound,

    // The detail names the rejected date
    [Problem(FailureStatusCode.BadRequest, true)]
    FutureDate,

    // The detail names the closed period and the date
    [Problem(FailureStatusCode.BadRequest, true)]
    PeriodClosed
}