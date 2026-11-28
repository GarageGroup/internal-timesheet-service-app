using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetModifyMetadata;

public enum TimesheetCreateFailureCode
{
    Unknown,

    [Problem(FailureStatusCode.BadRequest, true)]
    BadRequest,

    [Problem(FailureStatusCode.BadRequest, FailureCode.UnexpectedProjectTypeMessage)]
    UnexpectedProjectType,

    [Problem(FailureStatusCode.Forbidden, FailureCode.ForbiddenMessage)]
    Forbidden,

    [Problem(FailureStatusCode.NotFound, FailureCode.ProjectNotFoundMessage)]
    ProjectNotFound,

    [Problem(FailureStatusCode.BadRequest, FailureCode.EmptyDescriptionMessage)]
    EmptyDescription,

    // The detail names the rejected date
    [Problem(FailureStatusCode.BadRequest, true)]
    FutureDate,

    // The detail names the closed period and the date
    [Problem(FailureStatusCode.BadRequest, true)]
    PeriodClosed
}