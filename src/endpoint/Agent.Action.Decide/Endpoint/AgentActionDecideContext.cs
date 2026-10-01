namespace GarageGroup.Internal.Timesheet;

public sealed record class AgentActionDecideContext(
    IAgentTimesheetSetGetFunc TimesheetSetGetFunc,
    AgentActionDecideOption Option);
