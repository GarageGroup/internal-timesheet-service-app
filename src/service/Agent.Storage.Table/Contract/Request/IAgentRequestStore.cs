using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentRequestStore
{
    ValueTask<Result<AgentRequestGetOut, Failure<AgentRequestStoreFailureCode>>> CreateAsync(
        AgentUserContext context,
        AgentRequestCreateIn input,
        CancellationToken cancellationToken);

    ValueTask<Result<AgentRequestGetOut, Failure<AgentRequestStoreFailureCode>>> GetAsync(
        AgentUserContext context,
        string requestId,
        CancellationToken cancellationToken);

    ValueTask<Result<Unit, Failure<AgentRequestStoreFailureCode>>> UpdateAsync(
        AgentUserContext context,
        AgentRequestUpdateIn input,
        CancellationToken cancellationToken);
}
