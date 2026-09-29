namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentMessageOut(string Text, AgentTimesheetCreatePrepareOut? PreparedAction = null);
