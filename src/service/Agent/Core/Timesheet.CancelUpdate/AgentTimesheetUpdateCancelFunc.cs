namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetUpdateCancelFunc(
    IAgentTimesheetUpdateActionStore actionStore,
    IDateProvider dateProvider) : IAgentTimesheetUpdateCancelFunc;
