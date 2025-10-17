using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTag
{
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

    internal static DbParameterFilter BuildProjectFilter(Guid projectId)
        =>
        new($"{AliasName}.regardingobjectid", DbFilterOperator.Equal, projectId, "projectId");

    internal static DbLikeFilter BuildDescriptionFilter(string tagStartSymbol)
        =>
        new(DescriptionFieldName, $"%{tagStartSymbol}%", "description");

    internal static DbParameterFilter BuildMinDateFilter(DateOnly minDate)
        =>
        new(DateFieldName, DbFilterOperator.GreaterOrEqual, minDate.ToString(DateFormat), "minDate");

    internal static DbParameterFilter BuildMaxDateFilter(DateOnly maxDate)
        =>
        new(DateFieldName, DbFilterOperator.LessOrEqual, maxDate.ToString(DateFormat), "maxDate");
}