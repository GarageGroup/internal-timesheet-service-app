using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using static UserSignInMetadata;

public enum UserSignInFailureCode
{
    Unknown,

    [Problem(FailureStatusCode.NotFound, FailureCode.SystemUserNotFoundMessage)]
    SystemUserNotFound,

    [Problem(FailureStatusCode.BadRequest, FailureCode.InvalidTelegramDataMessage)]
    InvalidTelegramData,

    [Problem(FailureStatusCode.Conflict, FailureCode.TelegramUserAlreadyLinkedMessage)]
    TelegramUserAlreadyLinked
}
