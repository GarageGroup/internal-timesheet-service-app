using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetDeleteCancelFunc
{
    ValueTask<Result<AgentTimesheetDeleteCancelOut, Failure<AgentTimesheetDeleteCancelFailureCode>>> InvokeAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken);
}
