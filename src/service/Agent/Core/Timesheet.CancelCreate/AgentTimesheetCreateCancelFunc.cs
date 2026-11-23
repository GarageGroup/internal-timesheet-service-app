namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetCreateCancelFunc(
    IAgentActionStore actionStore,
    IDateProvider dateProvider) : IAgentTimesheetCreateCancelFunc;
