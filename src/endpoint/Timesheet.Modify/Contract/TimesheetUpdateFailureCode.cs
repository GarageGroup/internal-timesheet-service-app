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
    ProjectNotFound
}