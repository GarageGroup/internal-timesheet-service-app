using System;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentConversationTableStore
{
    private const string MessagesPropertyName = "Messages";

    public async ValueTask<Result<AgentConversationGetOut, Failure<AgentConversationStoreFailureCode>>> GetAsync(
        AgentUserContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await tableClient.GetEntityIfExistsAsync<TableEntity>(
                GetPartitionKey(context),
                GetRowKey(context),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (response.HasValue is false)
            {
                return new AgentConversationGetOut(default, null);
            }

            var entity = response.Value!;
            var messages = JsonSerializer.Deserialize<AgentChatMessage[]>(entity.GetString(MessagesPropertyName) ?? "[]") ?? [];

            return new AgentConversationGetOut(messages, entity.ETag.ToString());
        }
        catch (Exception exception) when (exception is RequestFailedException or JsonException)
        {
            return Failure.Create(AgentConversationStoreFailureCode.Unknown, "Failed to load agent conversation", exception);
        }
    }

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
                _ = await tableClient.AddEntityAsync(entity, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _ = await tableClient.UpdateEntityAsync(
                    entity,
                    new(expectedVersion),
                    TableUpdateMode.Replace,
                    cancellationToken).ConfigureAwait(false);
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

    private static string GetPartitionKey(AgentUserContext context)
        =>
        context.BotId.ToString(CultureInfo.InvariantCulture);

    private static string GetRowKey(AgentUserContext context)
        =>
        $"{context.TelegramUserId.ToString(CultureInfo.InvariantCulture)}-{context.TelegramChatId.ToString(CultureInfo.InvariantCulture)}";
}
