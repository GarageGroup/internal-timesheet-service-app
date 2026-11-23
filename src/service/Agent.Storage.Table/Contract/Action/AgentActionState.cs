namespace GarageGroup.Internal.Timesheet;

public enum AgentActionState
{
    Pending,
    Executing,
    Succeeded,
    Failed,
    Cancelled,
    Expired,
    Indeterminate
}
