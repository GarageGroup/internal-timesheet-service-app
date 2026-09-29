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
}
