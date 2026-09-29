namespace GarageGroup.Internal.Timesheet;

public interface IAgentKernelFactory
{
    AgentKernelScope Create(AgentUserContext context);
}
