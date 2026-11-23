namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetCreateConfirmFunc(
    IAgentActionStore actionStore,
    ITimesheetCreateFunc timesheetCreateFunc,
    IDateProvider dateProvider) : IAgentTimesheetCreateConfirmFunc;
