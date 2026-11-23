namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetUpdatePrepareFunc(
    IAgentTimesheetSetGetFunc timesheetSetGetFunc,
    IProjectSetGetFunc projectSetGetFunc,
    IAgentTimesheetUpdateActionStore actionStore,
    IDateProvider dateProvider,
    AgentTimesheetUpdatePrepareOption option) : IAgentTimesheetUpdatePrepareFunc;
