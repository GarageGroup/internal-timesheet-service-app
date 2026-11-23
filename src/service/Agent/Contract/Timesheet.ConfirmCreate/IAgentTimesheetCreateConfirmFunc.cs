using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetCreateConfirmFunc
{
    ValueTask<Result<AgentTimesheetCreateConfirmOut, Failure<AgentTimesheetCreateConfirmFailureCode>>> InvokeAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken);
}
