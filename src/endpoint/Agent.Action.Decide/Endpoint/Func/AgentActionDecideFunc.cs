namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentActionDecideFunc(
    IAgentUserContextResolver userContextResolver,
    IAgentTimesheetCreateConfirmFunc createConfirmFunc,
    IAgentTimesheetCreateCancelFunc createCancelFunc,
    IAgentTimesheetDeleteConfirmFunc deleteConfirmFunc,
    IAgentTimesheetDeleteCancelFunc deleteCancelFunc,
    IAgentTimesheetUpdateConfirmFunc updateConfirmFunc,
    IAgentTimesheetUpdateCancelFunc updateCancelFunc,
    AgentActionDecideOption option) : IAgentActionDecideFunc;
