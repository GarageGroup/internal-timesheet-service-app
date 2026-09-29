using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetCreatePrepareFunc
{
    ValueTask<Result<AgentTimesheetCreatePrepareOut, Failure<AgentTimesheetCreatePrepareFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentTimesheetCreatePrepareIn input,
        CancellationToken cancellationToken);
}
