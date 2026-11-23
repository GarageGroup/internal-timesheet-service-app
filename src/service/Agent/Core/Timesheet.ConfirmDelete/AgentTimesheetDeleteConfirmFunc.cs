namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetDeleteConfirmFunc(
    IAgentTimesheetDeleteActionStore actionStore,
    ITimesheetDeleteFunc timesheetDeleteFunc,
    IDateProvider dateProvider) : IAgentTimesheetDeleteConfirmFunc;
