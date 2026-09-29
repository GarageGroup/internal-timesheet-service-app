namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetCreatePrepareFunc(
    IAgentProjectSetSearchFunc projectSetSearchFunc,
    IAgentActionStore actionStore,
    IDateProvider dateProvider,
    AgentTimesheetCreatePrepareOption option) : IAgentTimesheetCreatePrepareFunc;
