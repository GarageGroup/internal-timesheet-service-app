using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentProjectSetSearchFunc
{
    ValueTask<Result<AgentProjectSetSearchOut, Failure<AgentProjectSetSearchFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentProjectSetSearchIn input,
        CancellationToken cancellationToken);
}
