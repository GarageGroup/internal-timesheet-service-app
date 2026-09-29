using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Azure;

namespace GarageGroup.Internal.Timesheet;

partial class AgentActionTableApi
{
    public async ValueTask<Result<AgentTimesheetCreateAction?, Failure<AgentActionStoreFailureCode>>> GetAsync(
        AgentUserContext context,
        Guid actionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = await tableApi.GetAsync(
                GetPartitionKey(context),
                GetRowKey(actionId),
                cancellationToken).ConfigureAwait(false);

            if (entity is null || IsAnotherOwner())
            {
                return default(AgentTimesheetCreateAction?);
            }

            var date = DateOnly.ParseExact(
                GetRequiredString(entity.GetString(DatePropertyName), DatePropertyName),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);
            var action = new AgentTimesheetCreateAction(
                actionId,
                date,
                GetRequiredValue(entity.GetGuid(ProjectIdPropertyName), ProjectIdPropertyName),
                entity.GetString(ProjectNamePropertyName),
                (ProjectType)GetRequiredValue(entity.GetInt32(ProjectTypePropertyName), ProjectTypePropertyName),
                decimal.Parse(
                    GetRequiredString(entity.GetString(DurationPropertyName), DurationPropertyName),
                    CultureInfo.InvariantCulture),
                entity.GetString(DescriptionPropertyName),
                GetRequiredValue(entity.GetDateTimeOffset(CreatedAtPropertyName), CreatedAtPropertyName),
                GetRequiredValue(entity.GetDateTimeOffset(ExpiresAtPropertyName), ExpiresAtPropertyName),
                (AgentActionState)GetRequiredValue(entity.GetInt32(StatePropertyName), StatePropertyName),
                entity.ETag.ToString());

            return action;

            bool IsAnotherOwner()
                =>
                entity.GetInt64(TelegramUserIdPropertyName).Equals(context.TelegramUserId) is false ||
                entity.GetInt64(TelegramChatIdPropertyName).Equals(context.TelegramChatId) is false ||
                entity.GetGuid(BindingIdPropertyName).Equals(context.BindingId) is false ||
                entity.GetGuid(CrmSystemUserIdPropertyName).Equals(context.CrmSystemUserId) is false ||
                entity.GetGuid(EntraObjectIdPropertyName).Equals(context.EntraObjectId) is false;
        }
        catch (Exception exception) when (exception is RequestFailedException or FormatException or InvalidOperationException)
        {
            return Failure.Create(AgentActionStoreFailureCode.Unknown, "Failed to load agent action", exception);
        }
    }

    private static string GetRequiredString(string? value, string propertyName)
    {
        if (value is not null)
        {
            return value;
        }

        throw new InvalidOperationException($"Agent action property '{propertyName}' is not specified");
    }

    private static T GetRequiredValue<T>(T? value, string propertyName)
        where T : struct
    {
        if (value is T result)
        {
            return result;
        }

        throw new InvalidOperationException($"Agent action property '{propertyName}' is not specified");
    }
}
