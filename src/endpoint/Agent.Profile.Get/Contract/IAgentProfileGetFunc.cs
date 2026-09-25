using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

[Endpoint("GetAgentProfile", EndpointMethod.Post, "/internal/agent/profile", Summary = "Get Telegram user profile for the agent")]
[EndpointTag("Agent")]
public interface IAgentProfileGetFunc
{
    ValueTask<Result<AgentProfileGetOut, Failure<AgentProfileGetFailureCode>>> InvokeAsync(
        AgentProfileGetIn input,
        CancellationToken cancellationToken);
}
