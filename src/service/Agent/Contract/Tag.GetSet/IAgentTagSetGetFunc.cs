using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTagSetGetFunc
{
    ValueTask<Result<AgentTagSetGetOut, Failure<AgentTagSetGetFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentTagSetGetIn input,
        CancellationToken cancellationToken);
}
