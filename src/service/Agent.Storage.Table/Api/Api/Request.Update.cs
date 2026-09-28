using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentRequestTableApi
{
    public async ValueTask<Result<Unit, Failure<AgentRequestStoreFailureCode>>> UpdateAsync(
        AgentUserContext context,
        AgentRequestUpdateIn input,
        CancellationToken cancellationToken)
    {
        var currentResult = await GetAsync(context, input.RequestId, cancellationToken).ConfigureAwait(false);
        if (currentResult.IsFailure)
        {
            return currentResult.FailureOrThrow();
        }

        var current = currentResult.SuccessOrThrow();
        if (string.Equals(current.Version, input.ExpectedVersion, StringComparison.Ordinal) is false ||
            current.Status != input.ExpectedStatus)
        {
            return Failure.Create(AgentRequestStoreFailureCode.Conflict, "Agent request version or status conflict");
        }

        var entity = CreateEntity(
            context,
            new(current.TelegramUpdateId, current.Text, current.Locale),
            current.RequestId,
            input.Status,
            input.ResponseText,
            input.FailureCode);

        try
        {
            await tableApi.UpdateAsync(entity, new(input.ExpectedVersion), cancellationToken).ConfigureAwait(false);

            return Unit.Value;
        }
        catch (RequestFailedException exception) when (
            exception.Status is (int)HttpStatusCode.Conflict or (int)HttpStatusCode.PreconditionFailed)
        {
            return Failure.Create(AgentRequestStoreFailureCode.Conflict, "Agent request version conflict", exception);
        }
        catch (RequestFailedException exception) when (exception.Status is (int)HttpStatusCode.NotFound)
        {
            return Failure.Create(AgentRequestStoreFailureCode.NotFound, "Agent request was not found", exception);
        }
        catch (RequestFailedException exception)
        {
            return Failure.Create(AgentRequestStoreFailureCode.Unknown, "Failed to update agent request", exception);
        }
    }
}
