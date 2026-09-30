namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetUpdateConfirmFunc(
    IAgentTimesheetUpdateActionStore actionStore,
    ITimesheetUpdateFunc timesheetUpdateFunc,
    IDateProvider dateProvider) : IAgentTimesheetUpdateConfirmFunc;
