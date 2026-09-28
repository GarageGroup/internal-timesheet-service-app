using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public enum AgentMessageSendFailureCode
{
    Unknown,

    [Problem(FailureStatusCode.BadRequest, "Telegram identity is invalid")]
    InvalidIdentity,

    [Problem(FailureStatusCode.NotFound, "Telegram user is not linked")]
    UserNotLinked,

    [Problem(FailureStatusCode.Forbidden, "Telegram user binding is unavailable")]
    UserUnavailable,

    [Problem(FailureStatusCode.BadRequest, "Agent message is invalid")]
    InvalidMessage,

    EmptyResponse,

    [Problem(FailureStatusCode.Conflict, "Agent conversation was changed by another request")]
    ConversationConflict
}
