using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public enum TimesheetDeleteFailureCode
{
    Unknown,

    [Problem(FailureStatusCode.BadRequest, true)]
    BadRequest
}