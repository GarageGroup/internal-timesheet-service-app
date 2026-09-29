using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetDeleteConfirmFunc
{
    ValueTask<Result<AgentTimesheetDeleteConfirmOut, Failure<AgentTimesheetDeleteConfirmFailureCode>>> InvokeAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken);
}
