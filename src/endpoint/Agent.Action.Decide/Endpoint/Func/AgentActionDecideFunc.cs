namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentActionDecideFunc(
    IAgentUserContextResolver userContextResolver,
    IAgentTimesheetCreateConfirmFunc confirmFunc,
    IAgentTimesheetCreateCancelFunc cancelFunc,
    AgentActionDecideOption option) : IAgentActionDecideFunc;
