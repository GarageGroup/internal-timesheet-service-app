using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbAgentUserBinding
{
    internal static DbCombinedFilter BuildFilter(long botId, long telegramUserId)
        =>
        new(DbLogicalOperator.And)
        {
            Filters =
            [
                new DbRawFilter($"{BindingAlias}.statecode = 0"),
                new DbParameterFilter($"{BindingAlias}.gg_bot_id", DbFilterOperator.Equal, botId, "botId"),
                new DbParameterFilter($"{BindingAlias}.gg_chat_id", DbFilterOperator.Equal, telegramUserId, "telegramUserId")
            ]
        };
}
