using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentKernelFactory
{
    Kernel Create(AgentUserContext context);
}
