using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public enum AgentProfileGetFailureCode
{
    Unknown,

    [Problem(FailureStatusCode.BadRequest, "Telegram identity is invalid")]
    InvalidIdentity,

    [Problem(FailureStatusCode.NotFound, "Telegram user is not linked")]
    UserNotLinked,

    [Problem(FailureStatusCode.Forbidden, "Telegram user binding is unavailable")]
    UserUnavailable,

    [Problem(FailureStatusCode.NotFound, "Profile was not found")]
    ProfileNotFound
}
