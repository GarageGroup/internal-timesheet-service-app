using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetUpdateActionStore
{
    ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> CreateAsync(
        AgentUserContext context,
        AgentTimesheetUpdateAction action,
        CancellationToken cancellationToken);

    ValueTask<Result<AgentTimesheetUpdateAction?, Failure<AgentActionStoreFailureCode>>> GetUpdateAsync(
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
