namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentTimesheetCreateConfirmFunc(
    IAgentActionStore actionStore,
    IDateProvider dateProvider) : IAgentTimesheetCreateConfirmFunc;
