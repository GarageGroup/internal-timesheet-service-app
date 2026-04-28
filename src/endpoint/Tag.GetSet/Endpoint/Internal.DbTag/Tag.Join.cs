using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTag
{
    internal static DbJoinedTable BuildOwnerJoinedTable(Guid ownerAzureUserId)
        =>
        new(
            type: DbJoinType.Inner,
            tableName: "systemuser",
            tableAlias: AliasUser,
            filter: new DbCombinedFilter(DbLogicalOperator.And)
            {
                Filters =
                [
                    OwnerJoinedFilter,
                    BuildOwnerFilter(ownerAzureUserId)
                ]
            });
}