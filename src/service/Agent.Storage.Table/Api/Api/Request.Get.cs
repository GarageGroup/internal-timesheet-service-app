using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;

namespace GarageGroup.Internal.Timesheet;

partial class AgentRequestTableApi
{
    public async ValueTask<Result<AgentRequestGetOut, Failure<AgentRequestStoreFailureCode>>> GetAsync(
        AgentUserContext context,
        string requestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = await tableApi.GetAsync(
                GetRequestPartitionKey(context),
                requestId,
                cancellationToken).ConfigureAwait(false);

            if (entity is null ||
                entity.GetInt64(TelegramUserIdPropertyName) != context.TelegramUserId ||
                entity.GetInt64(TelegramChatIdPropertyName) != context.TelegramChatId)
            {
                return Failure.Create(AgentRequestStoreFailureCode.NotFound, "Agent request was not found");
            }

            return MapEntity(entity);
        }
        catch (Exception exception) when (exception is RequestFailedException or ArgumentException or InvalidOperationException)
        {
            return Failure.Create(AgentRequestStoreFailureCode.Unknown, "Failed to load agent request", exception);
        }
    }
}
