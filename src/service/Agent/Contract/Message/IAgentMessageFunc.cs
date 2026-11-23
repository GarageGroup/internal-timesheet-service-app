using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentMessageFunc
{
    ValueTask<Result<AgentMessageOut, Failure<AgentMessageFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentMessageIn input,
        CancellationToken cancellationToken);
}
