using System;
using System.Runtime.CompilerServices;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Agent.Action.Decide.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentActionDecideDependency
{
    public static Dependency<AgentActionDecideEndpoint> UseAgentActionDecideEndpoint(
        this Dependency<
            IAgentUserContextResolver,
            IAgentTimesheetCreateConfirmFunc,
            IAgentTimesheetCreateCancelFunc,
            AgentActionDecideOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentActionDecideFunc>(CreateFunc).Map(AgentActionDecideEndpoint.Resolve);

        static AgentActionDecideFunc CreateFunc(
            IAgentUserContextResolver resolver,
            IAgentTimesheetCreateConfirmFunc confirmFunc,
            IAgentTimesheetCreateCancelFunc cancelFunc,
            AgentActionDecideOption option)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(confirmFunc);
            ArgumentNullException.ThrowIfNull(cancelFunc);
            ArgumentNullException.ThrowIfNull(option);

            return new(resolver, confirmFunc, cancelFunc, option);
        }
    }
}
