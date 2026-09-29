namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetDeletePrepareFunc(
    IAgentTimesheetSetGetFunc timesheetSetGetFunc,
    IAgentTimesheetDeleteActionStore actionStore,
    IDateProvider dateProvider,
    AgentTimesheetDeletePrepareOption option) : IAgentTimesheetDeletePrepareFunc;
