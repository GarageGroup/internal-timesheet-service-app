using System;
using System.Runtime.CompilerServices;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Agent.Message.Send.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentMessageSendDependency
{
    public static Dependency<AgentMessageSendEndpoint> UseAgentMessageSendEndpoint(
        this Dependency<IAgentUserContextResolver, IAgentConversationMessageFunc> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentMessageSendFunc>(CreateFunc).Map(AgentMessageSendEndpoint.Resolve);

        static AgentMessageSendFunc CreateFunc(
            IAgentUserContextResolver resolver,
            IAgentConversationMessageFunc messageFunc)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(messageFunc);

            return new(resolver, messageFunc);
        }
    }
}
