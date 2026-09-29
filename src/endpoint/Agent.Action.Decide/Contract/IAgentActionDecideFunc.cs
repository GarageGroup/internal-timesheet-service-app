using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[Endpoint(
    "DecideAgentAction",
    EndpointMethod.Post,
    "/internal/agent/actions/{actionId}/decision",
    Summary = "Confirm or cancel a prepared agent action")]
[EndpointTag("Agent")]
public interface IAgentActionDecideFunc
{
    ValueTask<Result<AgentActionDecideOut, Failure<AgentActionDecideFailureCode>>> InvokeAsync(
        AgentActionDecideIn input,
        CancellationToken cancellationToken);
}
