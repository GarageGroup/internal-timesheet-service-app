namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentProjectSetSearchFunc(
    IProjectSetSearchFunc projectSetSearchFunc,
    AgentProjectSetSearchOption option) : IAgentProjectSetSearchFunc;
