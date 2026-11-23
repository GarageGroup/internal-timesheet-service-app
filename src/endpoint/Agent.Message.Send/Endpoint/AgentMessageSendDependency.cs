using System;
using System.Runtime.CompilerServices;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Endpoint.Agent.Message.Send.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class AgentMessageSendDependency
{
    public static Dependency<AgentMessageSendEndpoint> UseAgentMessageSendEndpoint(
        this Dependency<IAgentUserContextResolver, IAgentConversationMessageFunc, IAgentAudioTranscribeFunc> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.UseAgentMessageSendFunc().Map(AgentMessageSendEndpoint.Resolve);
    }

    public static Dependency<IAgentMessageSendFunc> UseAgentMessageSendFunc(
        this Dependency<IAgentUserContextResolver, IAgentConversationMessageFunc, IAgentAudioTranscribeFunc> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IAgentMessageSendFunc>(CreateFunc);

        static AgentMessageSendFunc CreateFunc(
            IAgentUserContextResolver resolver,
            IAgentConversationMessageFunc messageFunc,
            IAgentAudioTranscribeFunc audioTranscribeFunc)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(messageFunc);
            ArgumentNullException.ThrowIfNull(audioTranscribeFunc);

            return new(resolver, messageFunc, audioTranscribeFunc);
        }
    }
}
