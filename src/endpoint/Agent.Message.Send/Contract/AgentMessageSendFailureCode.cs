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

    [Problem(FailureStatusCode.BadRequest, "Voice message is invalid")]
    InvalidAudio,

    [Problem(FailureStatusCode.BadRequest, "Voice message is too large")]
    AudioTooLarge,

    [Problem(FailureStatusCode.BadRequest, "Voice message format is unsupported")]
    UnsupportedAudioFormat,

    [Problem(FailureStatusCode.UnprocessableEntity, "Voice message could not be recognized")]
    EmptyTranscript,

    EmptyResponse,

    [Problem(FailureStatusCode.Conflict, "Agent conversation was changed by another request")]
    ConversationConflict
}
