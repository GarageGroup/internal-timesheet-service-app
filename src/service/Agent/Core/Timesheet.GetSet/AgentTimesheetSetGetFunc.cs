namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetSetGetFunc(
    ITimesheetSetGetFunc timesheetSetGetFunc,
    AgentTimesheetSetGetOption option) : IAgentTimesheetSetGetFunc;
