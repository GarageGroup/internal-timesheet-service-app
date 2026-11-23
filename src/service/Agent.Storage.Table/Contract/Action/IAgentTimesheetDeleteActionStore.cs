using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetDeleteActionStore
{
    ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> CreateAsync(
        AgentUserContext context,
        AgentTimesheetDeleteAction action,
        CancellationToken cancellationToken);

    ValueTask<Result<AgentTimesheetDeleteAction?, Failure<AgentActionStoreFailureCode>>> GetDeleteAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken);

    ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> UpdateStateAsync(
        AgentUserContext context,
        Guid actionId,
        string expectedVersion,
        AgentActionState expectedState,
        AgentActionState nextState,
        CancellationToken cancellationToken);
}
