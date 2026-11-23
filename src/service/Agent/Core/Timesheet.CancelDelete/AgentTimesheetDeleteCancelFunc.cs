namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetDeleteCancelFunc(
    IAgentTimesheetDeleteActionStore actionStore,
    IDateProvider dateProvider) : IAgentTimesheetDeleteCancelFunc;
