using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetUpdatePrepareFunc
{
    ValueTask<Result<AgentTimesheetUpdatePrepareOut, Failure<AgentTimesheetUpdatePrepareFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentTimesheetUpdatePrepareIn input,
        CancellationToken cancellationToken);
}
