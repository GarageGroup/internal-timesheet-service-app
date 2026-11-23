using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;

namespace GarageGroup.Internal.Timesheet;

partial class AgentConversationTableApi
{
    private const string MessagesPropertyName = "Messages";

    public async ValueTask<Result<AgentConversationGetOut, Failure<AgentConversationStoreFailureCode>>> GetAsync(
        AgentUserContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = await tableApi.GetAsync(
                GetPartitionKey(context),
                GetRowKey(context),
                cancellationToken).ConfigureAwait(false);

            if (entity is null)
            {
                return new AgentConversationGetOut(default, null);
            }

            var messagesJson = entity.GetString(MessagesPropertyName);
            if (string.IsNullOrEmpty(messagesJson))
            {
                throw new InvalidOperationException($"Required property '{MessagesPropertyName}' is missing");
            }

            var messages = JsonSerializer.Deserialize<AgentChatMessage[]>(messagesJson);
            if (messages is null)
            {
                throw new InvalidOperationException($"Required property '{MessagesPropertyName}' must be a JSON array");
            }

            return new AgentConversationGetOut(messages, entity.ETag.ToString());
        }
        catch (Exception exception) when (exception is RequestFailedException or JsonException or InvalidOperationException)
        {
            return Failure.Create(AgentConversationStoreFailureCode.Unknown, "Failed to load agent conversation", exception);
        }
    }
}
