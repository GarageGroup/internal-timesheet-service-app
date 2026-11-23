using GarageGroup.Infra;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface IAgentUserContextResolver
{
    ValueTask<Result<AgentUserContext, Failure<AgentUserContextResolveFailureCode>>> ResolveAsync(
        AgentUserIdentity identity,
        CancellationToken cancellationToken);
}
