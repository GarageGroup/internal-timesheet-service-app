using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentActionTableApi
{
    public async ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> UpdateStateAsync(
        AgentUserContext context,
        Guid actionId,
        string expectedVersion,
        AgentActionState expectedState,
        AgentActionState nextState,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = await tableApi.GetAsync(
                GetPartitionKey(context),
                GetRowKey(actionId),
                cancellationToken).ConfigureAwait(false);

            if (entity is null ||
                IsAnotherOwner(entity, context) ||
                entity.ETag.ToString().Equals(expectedVersion, StringComparison.Ordinal) is false ||
                entity.GetInt32(StatePropertyName).Equals((int)expectedState) is false)
            {
                return Failure.Create(AgentActionStoreFailureCode.Conflict, "Agent action state conflict");
            }

            entity[StatePropertyName] = (int)nextState;
            await tableApi.UpdateAsync(entity, entity.ETag, cancellationToken).ConfigureAwait(false);

            return Unit.Value;
        }
        catch (RequestFailedException exception) when (
            exception.Status is (int)HttpStatusCode.Conflict or (int)HttpStatusCode.PreconditionFailed or (int)HttpStatusCode.NotFound)
        {
            return Failure.Create(AgentActionStoreFailureCode.Conflict, "Agent action state conflict", exception);
        }
        catch (RequestFailedException exception)
        {
            return Failure.Create(AgentActionStoreFailureCode.Unknown, "Failed to update agent action state", exception);
        }
    }
}
