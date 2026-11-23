using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetCreateCancelFunc
{
    ValueTask<Result<AgentTimesheetCreateCancelOut, Failure<AgentTimesheetCreateCancelFailureCode>>> InvokeAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken);
}
