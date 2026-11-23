namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentMessageOut(
    string Text,
    AgentTimesheetCreatePrepareOut? PreparedCreateAction = null,
    AgentTimesheetDeletePrepareOut? PreparedDeleteAction = null,
    AgentTimesheetUpdatePrepareOut? PreparedUpdateAction = null);
