using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentTimesheetSetGetFunc
{
    ValueTask<Result<AgentTimesheetSetGetOut, Failure<AgentTimesheetSetGetFailureCode>>> InvokeAsync(
        AgentUserContext context,
        AgentTimesheetSetGetIn input,
        CancellationToken cancellationToken);
}
