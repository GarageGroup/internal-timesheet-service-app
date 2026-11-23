using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentPeriodSetGetFunc
{
    ValueTask<Result<AgentPeriodSetGetOut, Failure<AgentPeriodSetGetFailureCode>>> InvokeAsync(
        AgentUserContext context,
        CancellationToken cancellationToken);
}
