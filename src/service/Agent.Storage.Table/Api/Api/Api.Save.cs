using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentConversationTableApi
{
    public async ValueTask<Result<Unit, Failure<AgentConversationStoreFailureCode>>> SaveAsync(
        AgentUserContext context,
        string? expectedVersion,
        FlatArray<AgentChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var entity = new TableEntity(GetPartitionKey(context), GetRowKey(context))
        {
            [MessagesPropertyName] = JsonSerializer.Serialize(messages.AsEnumerable())
        };

        try
        {
            if (string.IsNullOrEmpty(expectedVersion))
            {
                await tableApi.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await tableApi.UpdateAsync(entity, new(expectedVersion), cancellationToken).ConfigureAwait(false);
            }

            return Unit.Value;
        }
        catch (RequestFailedException exception) when (
            exception.Status is (int)HttpStatusCode.Conflict or (int)HttpStatusCode.PreconditionFailed or (int)HttpStatusCode.NotFound)
        {
            return Failure.Create(AgentConversationStoreFailureCode.Conflict, "Agent conversation version conflict", exception);
        }
        catch (RequestFailedException exception)
        {
            return Failure.Create(AgentConversationStoreFailureCode.Unknown, "Failed to save agent conversation", exception);
        }
    }
}
