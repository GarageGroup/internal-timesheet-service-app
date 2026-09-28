using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace GarageGroup.Internal.Timesheet;

partial class AgentRequestTableApi
{
    public async ValueTask<Result<AgentRequestGetOut, Failure<AgentRequestStoreFailureCode>>> CreateAsync(
        AgentUserContext context,
        AgentRequestCreateIn input,
        CancellationToken cancellationToken)
    {
        var requestId = GetRequestId(context.BotId, input.TelegramUpdateId);
        var entity = new TableEntity(GetRequestPartitionKey(context), requestId)
        {
            [TelegramUserIdPropertyName] = context.TelegramUserId,
            [TelegramChatIdPropertyName] = context.TelegramChatId,
            [TelegramUpdateIdPropertyName] = input.TelegramUpdateId,
            [TextPropertyName] = input.Text,
            [StatusPropertyName] = AgentRequestStatus.Queued.ToString()
        };

        if (input.Locale is not null)
        {
            entity[LocalePropertyName] = input.Locale;
        }

        try
        {
            await tableApi.AddAsync(entity, cancellationToken).ConfigureAwait(false);

            return MapEntity(entity);
        }
        catch (RequestFailedException exception) when (exception.Status is (int)HttpStatusCode.Conflict)
        {
            return await GetDuplicateAsync(context, input, requestId, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception)
        {
            return Failure.Create(AgentRequestStoreFailureCode.Unknown, "Failed to create agent request", exception);
        }
    }

    private async ValueTask<Result<AgentRequestGetOut, Failure<AgentRequestStoreFailureCode>>> GetDuplicateAsync(
        AgentUserContext context,
        AgentRequestCreateIn input,
        string requestId,
        CancellationToken cancellationToken)
    {
        var duplicate = await GetAsync(context, requestId, cancellationToken).ConfigureAwait(false);
        if (duplicate.IsFailure)
        {
            return duplicate.FailureOrThrow().WithFailureCode(AgentRequestStoreFailureCode.Conflict);
        }

        var request = duplicate.SuccessOrThrow();
        if (request.TelegramUpdateId != input.TelegramUpdateId ||
            string.Equals(request.Text, input.Text, StringComparison.Ordinal) is false ||
            string.Equals(request.Locale, input.Locale, StringComparison.Ordinal) is false)
        {
            return Failure.Create(AgentRequestStoreFailureCode.Conflict, "Telegram update is already assigned to another request");
        }

        return request;
    }
}
