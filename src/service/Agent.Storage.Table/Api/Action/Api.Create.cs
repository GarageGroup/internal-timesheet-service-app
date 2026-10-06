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
    public ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> CreateAsync(
        AgentUserContext context,
        AgentTimesheetCreateAction action,
        CancellationToken cancellationToken)
    {
        var entity = BuildEntity(
            context, action.ActionId, AgentActionType.CreateTimesheet,
            action.Date, action.ProjectName, action.Duration, action.Description,
            action.CreatedAt, action.ExpiresAt);
        entity[ProjectIdPropertyName] = action.ProjectId;
        entity[ProjectTypePropertyName] = (int)action.ProjectType;

        return SaveAsync(entity, cancellationToken);
    }

    public ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> CreateAsync(
        AgentUserContext context,
        AgentTimesheetDeleteAction action,
        CancellationToken cancellationToken)
    {
        var entity = BuildEntity(
            context, action.ActionId, AgentActionType.DeleteTimesheet,
            action.Date, action.ProjectName, action.Duration, action.Description,
            action.CreatedAt, action.ExpiresAt);
        entity[TimesheetIdPropertyName] = action.TimesheetId;

        return SaveAsync(entity, cancellationToken);
    }

    public ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> CreateAsync(
        AgentUserContext context,
        AgentTimesheetUpdateAction action,
        CancellationToken cancellationToken)
    {
        var entity = BuildEntity(
            context, action.ActionId, AgentActionType.UpdateTimesheet,
            action.Date, action.ProjectName, action.Duration, action.Description,
            action.CreatedAt, action.ExpiresAt);
        entity[TimesheetIdPropertyName] = action.TimesheetId;
        entity[ProjectIdPropertyName] = action.ProjectId;
        entity[ProjectTypePropertyName] = (int)action.ProjectType;

        return SaveAsync(entity, cancellationToken);
    }

    private static TableEntity BuildEntity(
        AgentUserContext context,
        Guid actionId,
        AgentActionType actionType,
        DateOnly date,
        string projectName,
        decimal duration,
        string? description,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
        =>
        new(GetPartitionKey(context), GetRowKey(actionId))
        {
            [TelegramUserIdPropertyName] = context.TelegramUserId,
            [TelegramChatIdPropertyName] = context.TelegramChatId,
            [BindingIdPropertyName] = context.BindingId,
            [CrmSystemUserIdPropertyName] = context.CrmSystemUserId,
            [EntraObjectIdPropertyName] = context.EntraObjectId,
            [ActionTypePropertyName] = (int)actionType,
            [DatePropertyName] = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            [ProjectNamePropertyName] = projectName,
            [DurationPropertyName] = duration.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [DescriptionPropertyName] = description,
            [CreatedAtPropertyName] = createdAt,
            [ExpiresAtPropertyName] = expiresAt,
            [StatePropertyName] = (int)AgentActionState.Pending
        };

    private async ValueTask<Result<Unit, Failure<AgentActionStoreFailureCode>>> SaveAsync(
        TableEntity entity,
        CancellationToken cancellationToken)
    {
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
