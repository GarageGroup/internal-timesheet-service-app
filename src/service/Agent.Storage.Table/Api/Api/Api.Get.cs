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

            var messages = JsonSerializer.Deserialize<AgentChatMessage[]>(entity.GetString(MessagesPropertyName) ?? "[]") ?? [];

            return new AgentConversationGetOut(messages, entity.ETag.ToString());
        }
        catch (Exception exception) when (exception is RequestFailedException or JsonException)
        {
            return Failure.Create(AgentConversationStoreFailureCode.Unknown, "Failed to load agent conversation", exception);
        }
    }
}
