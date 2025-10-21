using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

partial record class DbProject
{
    internal static FlatArray<DbAppliedTable> BuildTimesheetDateDbAppliedTables(Guid userId, DateOnly minDate)
        => 
        [
            new(
                type: DbApplyType.Outer,
                alias: UserLastTimesheetDateAliasName,
                selectQuery: new("gg_timesheetactivity", "t")
                {
                    Top = 1,
                    SelectedFields = new("t.gg_date AS LastDay"),
                    Filter = new DbCombinedFilter(DbLogicalOperator.And)
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
                                            new DbRawFilter("t.ownerid = u.systemuserid"),
                                            new DbParameterFilter("u.azureactivedirectoryobjectid", DbFilterOperator.Equal, userId, "userId")
                                        ]
                                    }
                                }),
                            new DbParameterFilter("t.gg_date", DbFilterOperator.GreaterOrEqual, minDate.ToString("yyyy-MM-dd"), "minDate"),
                            new DbRawFilter("p.gg_projectid = t.regardingobjectid"),
                            new DbRawFilter($"t.regardingobjecttypecode = {ProjectType.Project:D}"),
                            new DbRawFilter("t.statecode = 0")
                        ]
                    },
                    Orders =
                    [
                        new("t.gg_date", DbOrderType.Descending)
                    ]
                }),
            new(
                type: DbApplyType.Outer,
                alias: LastTimesheetDateAliasName,
                selectQuery: new("gg_timesheetactivity", "t1")
                {
                    Top = 1,
                    SelectedFields = new("t1.gg_date AS LastDay"),
                    Filter = new DbCombinedFilter(DbLogicalOperator.And)
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
                                            new DbRawFilter("t1.ownerid = u.systemuserid"),
                                            new DbParameterFilter("u.azureactivedirectoryobjectid", DbFilterOperator.Equal, userId, "userId")
                                        ]
                                    }
                                }),
                            new DbParameterFilter("t1.gg_date", DbFilterOperator.GreaterOrEqual, minDate.ToString("yyyy-MM-dd"), "minDate"),
                            new DbRawFilter("p.gg_projectid = t1.regardingobjectid"),
                            new DbRawFilter($"t1.regardingobjecttypecode = {ProjectType.Project:D}"),
                            new DbRawFilter("t1.statecode = 0")
                        ]
                    },
                    Orders =
                    [
                        new("t1.gg_date", DbOrderType.Descending)
                    ]
                })
        ];
}