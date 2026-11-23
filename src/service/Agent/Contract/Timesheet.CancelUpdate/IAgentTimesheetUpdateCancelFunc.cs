using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetUpdateCancelFunc
{
    ValueTask<Result<AgentTimesheetUpdateCancelOut, Failure<AgentTimesheetUpdateCancelFailureCode>>> InvokeAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken);
}
