using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetUpdateConfirmFunc
{
    ValueTask<Result<AgentTimesheetUpdateConfirmOut, Failure<AgentTimesheetUpdateConfirmFailureCode>>> InvokeAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken);
}
