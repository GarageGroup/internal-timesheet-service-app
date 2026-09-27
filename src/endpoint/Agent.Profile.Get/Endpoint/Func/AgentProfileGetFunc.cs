using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class AgentProfileGetFunc(
    IAgentUserContextResolver userContextResolver,
    IProfileGetFunc profileGetFunc) : IAgentProfileGetFunc;
