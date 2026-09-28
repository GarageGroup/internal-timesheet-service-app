using System;
using GarageGroup.Infra;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [EndpointApplicationExtension]
    internal static Dependency<AgentMessageSendEndpoint> UseAgentMessageSendEndpoint()
        =>
        Pipeline.Pipe(
            UseSqlApi())
        .UseAgentUserContextResolver()
        .With(
            UseAgentConversationMessageFunc())
        .UseAgentMessageSendEndpoint();
}
