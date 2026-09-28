using System;
using Azure.Data.Tables;

namespace GarageGroup.Internal.Timesheet;

partial class AgentRequestTableApi
{
    private const string TelegramUserIdPropertyName = "TelegramUserId";

    private const string TelegramChatIdPropertyName = "TelegramChatId";

    private const string TelegramUpdateIdPropertyName = "TelegramUpdateId";

    private const string TextPropertyName = "Text";

    private const string LocalePropertyName = "Locale";

    private const string StatusPropertyName = "Status";

    private const string ResponseTextPropertyName = "ResponseText";

    private const string FailureCodePropertyName = "FailureCode";

    private static TableEntity CreateEntity(
        AgentUserContext context,
        AgentRequestCreateIn input,
        string requestId,
        AgentRequestStatus status,
        string? responseText = null,
        string? failureCode = null)
    {
        var entity = new TableEntity(GetRequestPartitionKey(context), requestId)
        {
            [TelegramUserIdPropertyName] = context.TelegramUserId,
            [TelegramChatIdPropertyName] = context.TelegramChatId,
            [TelegramUpdateIdPropertyName] = input.TelegramUpdateId,
            [TextPropertyName] = input.Text,
            [StatusPropertyName] = status.ToString()
        };

        SetOptionalProperty(entity, LocalePropertyName, input.Locale);
        SetOptionalProperty(entity, ResponseTextPropertyName, responseText);
        SetOptionalProperty(entity, FailureCodePropertyName, failureCode);

        return entity;
    }

    private static AgentRequestGetOut MapEntity(TableEntity entity)
        =>
        new(
            entity.RowKey,
            entity.GetInt64(TelegramUpdateIdPropertyName).GetValueOrDefault(),
            entity.GetString(TextPropertyName) ?? string.Empty,
            entity.GetString(LocalePropertyName),
            Enum.Parse<AgentRequestStatus>(entity.GetString(StatusPropertyName) ?? string.Empty),
            entity.GetString(ResponseTextPropertyName),
            entity.GetString(FailureCodePropertyName),
            entity.ETag.ToString());

    private static void SetOptionalProperty(TableEntity entity, string propertyName, string? value)
    {
        if (value is not null)
        {
            entity[propertyName] = value;
        }
    }
}
