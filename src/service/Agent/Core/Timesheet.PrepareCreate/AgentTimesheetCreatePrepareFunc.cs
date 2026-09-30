namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetCreatePrepareFunc(
    IProjectSetGetFunc projectSetGetFunc,
    IAgentActionStore actionStore,
    IDateProvider dateProvider,
    AgentTimesheetCreatePrepareOption option) : IAgentTimesheetCreatePrepareFunc;
