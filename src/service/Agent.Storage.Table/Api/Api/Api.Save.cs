using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentConversationTableApi
{
    // Azure Table permits at most 64 KiB per string property; leave room below 32K UTF-16 code units.
    private const int MaxMessagesPropertyLength = 30_000;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public async ValueTask<Result<Unit, Failure<AgentConversationStoreFailureCode>>> SaveAsync(
        AgentUserContext context,
        string? expectedVersion,
        FlatArray<AgentChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var entity = new TableEntity(GetPartitionKey(context), GetRowKey(context))
        {
            [MessagesPropertyName] = SerializeMessagesWithinLimit(messages)
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

    private static string SerializeMessagesWithinLimit(FlatArray<AgentChatMessage> messages)
    {
        var retainedMessages = messages.AsEnumerable().ToList();
        var messagesJson = JsonSerializer.Serialize(retainedMessages, SerializerOptions);

        while (messagesJson.Length > MaxMessagesPropertyLength && retainedMessages.Count > 1)
        {
            retainedMessages.RemoveAt(0);
            if (retainedMessages.Count > 1 && retainedMessages[0].Role is AgentChatMessageRole.Assistant)
            {
                retainedMessages.RemoveAt(0);
            }

            messagesJson = JsonSerializer.Serialize(retainedMessages, SerializerOptions);
        }

        if (messagesJson.Length <= MaxMessagesPropertyLength || retainedMessages.Count is 0)
        {
            return messagesJson;
        }

        var lastMessage = retainedMessages[0];
        var minLength = 0;
        var maxLength = lastMessage.Text.Length - 1;

        while (minLength <= maxLength)
        {
            var candidateLength = minLength + (maxLength - minLength) / 2;
            retainedMessages[0] = lastMessage with { Text = string.Concat(lastMessage.Text.AsSpan(0, candidateLength), "…") };
            var candidateJson = JsonSerializer.Serialize(retainedMessages, SerializerOptions);

            if (candidateJson.Length <= MaxMessagesPropertyLength)
            {
                messagesJson = candidateJson;
                minLength = candidateLength + 1;
            }
            else
            {
                maxLength = candidateLength - 1;
            }
        }

        return messagesJson;
    }
}
