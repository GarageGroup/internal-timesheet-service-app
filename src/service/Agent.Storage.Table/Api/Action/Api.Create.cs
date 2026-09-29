using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class AgentActionTableApi
{
    public async ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> CreateAsync(
        AgentUserContext context,
        AgentTimesheetCreateAction action,
        CancellationToken cancellationToken)
    {
        var entity = new TableEntity(GetPartitionKey(context), GetRowKey(action.ActionId))
        {
            [TelegramUserIdPropertyName] = context.TelegramUserId,
            [TelegramChatIdPropertyName] = context.TelegramChatId,
            [BindingIdPropertyName] = context.BindingId,
            [CrmSystemUserIdPropertyName] = context.CrmSystemUserId,
            [EntraObjectIdPropertyName] = context.EntraObjectId,
            [ActionTypePropertyName] = (int)AgentActionType.CreateTimesheet,
            [DatePropertyName] = action.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            [ProjectIdPropertyName] = action.ProjectId,
            [ProjectNamePropertyName] = action.ProjectName,
            [ProjectTypePropertyName] = (int)action.ProjectType,
            [DurationPropertyName] = action.Duration.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [DescriptionPropertyName] = action.Description,
            [CreatedAtPropertyName] = action.CreatedAt,
            [ExpiresAtPropertyName] = action.ExpiresAt,
            [StatePropertyName] = (int)AgentActionState.Pending
        };

        try
        {
            await tableApi.AddAsync(entity, cancellationToken).ConfigureAwait(false);

            return Unit.Value;
        }
        catch (RequestFailedException exception) when (exception.Status is (int)HttpStatusCode.Conflict)
        {
            return Failure.Create(AgentActionStoreFailureCode.Conflict, "Agent action already exists", exception);
        }
        catch (RequestFailedException exception)
        {
            return Failure.Create(AgentActionStoreFailureCode.Unknown, "Failed to create agent action", exception);
        }
    }

    public async ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> CreateAsync(
        AgentUserContext context,
        AgentTimesheetDeleteAction action,
        CancellationToken cancellationToken)
    {
        var entity = new TableEntity(GetPartitionKey(context), GetRowKey(action.ActionId))
        {
            [TelegramUserIdPropertyName] = context.TelegramUserId,
            [TelegramChatIdPropertyName] = context.TelegramChatId,
            [BindingIdPropertyName] = context.BindingId,
            [CrmSystemUserIdPropertyName] = context.CrmSystemUserId,
            [EntraObjectIdPropertyName] = context.EntraObjectId,
            [ActionTypePropertyName] = (int)AgentActionType.DeleteTimesheet,
            [TimesheetIdPropertyName] = action.TimesheetId,
            [DatePropertyName] = action.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            [ProjectNamePropertyName] = action.ProjectName,
            [DurationPropertyName] = action.Duration.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [DescriptionPropertyName] = action.Description,
            [CreatedAtPropertyName] = action.CreatedAt,
            [ExpiresAtPropertyName] = action.ExpiresAt,
            [StatePropertyName] = (int)AgentActionState.Pending
        };

        try
        {
            await tableApi.AddAsync(entity, cancellationToken).ConfigureAwait(false);

            return Unit.Value;
        }
        catch (RequestFailedException exception) when (exception.Status is (int)HttpStatusCode.Conflict)
        {
            return Failure.Create(AgentActionStoreFailureCode.Conflict, "Agent action already exists", exception);
        }
        catch (RequestFailedException exception)
        {
            return Failure.Create(AgentActionStoreFailureCode.Unknown, "Failed to create agent action", exception);
        }
    }
}
