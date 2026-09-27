using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentLastProjectSetGetFunc
{
    ValueTask<Result<AgentLastProjectSetGetOut, Failure<AgentLastProjectSetGetFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentLastProjectSetGetIn input,
        CancellationToken cancellationToken);
}
