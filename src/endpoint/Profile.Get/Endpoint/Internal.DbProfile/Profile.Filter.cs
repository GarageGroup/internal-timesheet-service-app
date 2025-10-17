using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

partial record class DbProfile
{
    private static readonly DbRawFilter SystemUserIdFilter
        =
        new($"{AliasName}.gg_systemuser_id = u.systemuserid");

    internal static DbCombinedFilter BuildDefaultFilter(Guid systemUserId, long botId)
        =>
        new(DbLogicalOperator.And)
        {
            Filters =
            [
                new DbExistsFilter(
                    selectQuery: new("systemuser", "u")
                    {
                        Top = 1,
                        SelectedFields = new("1"),
                        Filter = new DbCombinedFilter(DbLogicalOperator.And)
                        {
                            Filters =
                            [
                                SystemUserIdFilter,
                                new DbParameterFilter("u.azureactivedirectoryobjectid", DbFilterOperator.Equal, systemUserId, "systemUserId")
                            ]
                        }
                    }),
                new DbParameterFilter($"{AliasName}.gg_bot_id", DbFilterOperator.Equal, botId, "botId")
            ]
        };
}
