using GarageGroup.Infra;
using System;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class UserBindingJson
{
    private const string EntityPluralName = "gg_telegram_bot_users";

    private const string SystemUserIdFieldName = "_gg_systemuser_id_value";

    private const string BotIdFieldName = "gg_bot_id";

    private const string ChatIdFieldName = "gg_chat_id";

    internal static DataverseEntitySetGetIn BuildDataverseInput(long botId, long telegramUserId)
        =>
        new(
            entityPluralName: EntityPluralName,
            selectFields: [SystemUserIdFieldName],
            filter: new DataverseLogicalFilter(DataverseLogicalOperator.And)
            {
                Filters =
                [
                    new DataverseComparisonFilter(BotIdFieldName, DataverseComparisonOperator.Equal, botId.ToString()),
                    new DataverseComparisonFilter(ChatIdFieldName, DataverseComparisonOperator.Equal, telegramUserId.ToString()),
                    new DataverseComparisonFilter("statecode", DataverseComparisonOperator.Equal, 0)
                ]
            });

    [JsonPropertyName(SystemUserIdFieldName)]
    public Guid CrmSystemUserId { get; init; }
}
