namespace GarageGroup.Internal.Timesheet;

public enum AgentRequestStatus
{
    Queued,

    Running,

    AwaitingConfirmation,

    Completed,

    Failed,

    Indeterminate
}
