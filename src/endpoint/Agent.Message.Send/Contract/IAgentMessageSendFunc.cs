using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[Endpoint("SendAgentMessage", EndpointMethod.Post, "/internal/agent/messages", Summary = "Send a message to the timesheet agent")]
[EndpointTag("Agent")]
public interface IAgentMessageSendFunc
{
    ValueTask<Result<AgentMessageSendOut, Failure<AgentMessageSendFailureCode>>> InvokeAsync(
        AgentMessageSendIn input,
        CancellationToken cancellationToken);
}
