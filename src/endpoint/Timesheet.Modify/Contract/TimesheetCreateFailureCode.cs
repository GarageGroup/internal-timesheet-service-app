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
}