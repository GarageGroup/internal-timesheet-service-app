using System;
using System.Linq;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheet
{
    internal static readonly DbParameterArrayFilter AllowedProjectTypeSetFilter
        =
        new(
            fieldName: $"{AliasName}.regardingobjecttypecode",
            @operator: DbArrayFilterOperator.In,
            fieldValues: Enum.GetValues<ProjectType>().Select(AsInt32).OrderBy(Pipeline.Pipe).Select(AsObject).ToFlatArray(),
            parameterPrefix: "projectTypeCode");

    internal static DbExistsFilter BuildOwnerFilter(Guid ownerId)
        =>
        new(
            selectQuery: new("systemuser", "u")
            {
                Top = 1,
                SelectedFields = new("1"),
                Filter = new DbCombinedFilter(DbLogicalOperator.And)
                {
                    Filters =
                    [
                        new DbRawFilter($"{AliasName}.ownerid = u.systemuserid"),
                        new DbParameterFilter("u.azureactivedirectoryobjectid", DbFilterOperator.Equal, ownerId, "ownerId")
                    ]
                }
            });

    internal static DbCombinedFilter BuildDateFilter(DateOnly dateFrom, DateOnly dateTo)
        =>
        new(DbLogicalOperator.And)
        {
            Filters =
            [
                new DbParameterFilter($"{AliasName}.gg_date", DbFilterOperator.GreaterOrEqual, dateFrom.ToString(DateFormat), "dateFrom"),
                new DbParameterFilter($"{AliasName}.gg_date", DbFilterOperator.LessOrEqual, dateTo.ToString(DateFormat), "dateTo")
            ]
        };

    private static int AsInt32(ProjectType type)
        =>
        (int)type;

    private static object? AsObject(int type)
        =>
        type;
}