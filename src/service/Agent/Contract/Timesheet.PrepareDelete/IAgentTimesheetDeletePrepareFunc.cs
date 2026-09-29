using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetDeletePrepareFunc
{
    ValueTask<Result<AgentTimesheetDeletePrepareOut, Failure<AgentTimesheetDeletePrepareFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentTimesheetDeletePrepareIn input,
        CancellationToken cancellationToken);
}
