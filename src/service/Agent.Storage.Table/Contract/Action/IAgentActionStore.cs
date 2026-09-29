using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentActionStore
{
    ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> CreateAsync(
        AgentUserContext context,
        AgentTimesheetCreateAction action,
        CancellationToken cancellationToken);

    ValueTask<Result<AgentTimesheetCreateAction?, Failure<AgentActionStoreFailureCode>>> GetAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken);
}
