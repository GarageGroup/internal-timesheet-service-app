namespace GarageGroup.Internal.Timesheet;

public enum AgentUserContextResolveFailureCode
{
    Unknown,

    InvalidIdentity,

    UnsupportedChat,

    UserNotLinked,

    AmbiguousBinding,

    BindingSignedOut,

    UserDisabled,

    MissingEntraObjectId
}
