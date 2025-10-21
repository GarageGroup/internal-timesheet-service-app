using System;
using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Project.LastSetGet.Test;

partial class LastProjectSetGetFuncSource
{
    public static TheoryData<LastProjectSetGetIn, LastProjectSetGetOption, DateOnly, DbSelectQuery> InputGetLastTestData
        =>
        new()
        {
            {
                new(
                    systemUserId: new("bef33be0-99f5-4018-ba80-3366ec9ec1fd"),
                    top: 7),
                new(lastDaysPeriod: 30),
                new(2024, 06, 04),
                new("gg_timesheetactivity", "t")
                {
                    Top = 7,
                    SelectedFields =
                    [
                        "t.regardingobjectid AS ProjectId",
                        "t.regardingobjecttypecode AS ProjectTypeCode",
                        "MAX(t.regardingobjectidname) AS ProjectName",
                        "MAX(p.gg_comment) AS ProjectComment",
                        "(SELECT TOP 1 sub.subject " +
                        "FROM gg_timesheetactivity sub " +
                        "WHERE sub.regardingobjectid = t.regardingobjectid " +
                        "ORDER BY sub.createdon DESC) AS Subject",
                        "MAX(t.gg_date) AS MaxDate",
                        "MAX(t.createdon) AS MaxCreatedOn"
                    ],
                    JoinedTables =
                    [
                        new(
                            type: DbJoinType.Left,
                            tableName: "gg_project",
                            tableAlias: "p",
                            rawFilter: "t.regardingobjecttypecode = 10912 AND t.regardingobjectid = p.gg_projectid")
                    ],
                    Filter = new DbCombinedFilter(DbLogicalOperator.And)
                    {
                        Filters =
                        [
                            new DbExistsFilter(
                                selectQuery: new DbSelectQuery("systemuser", "u")
                                {
                                    Top = 1,
                                    SelectedFields = new("1"),
                                    Filter = new DbCombinedFilter(DbLogicalOperator.And)
                                    {
                                        Filters =
                                        [
                                            new DbRawFilter("t.ownerid = u.systemuserid"),
                                            new DbParameterFilter(
                                                fieldName: "u.azureactivedirectoryobjectid",
                                                @operator: DbFilterOperator.Equal,
                                                fieldValue: Guid.Parse("bef33be0-99f5-4018-ba80-3366ec9ec1fd"),
                                                parameterName: "ownerId")
                                        ]
                                    }
                                }),
                            new DbParameterFilter(
                                fieldName: "t.gg_date",
                                @operator: DbFilterOperator.GreaterOrEqual,
                                fieldValue: "2024-05-05",
                                parameterName: "minDate"),
                            new DbParameterArrayFilter(
                                fieldName: "t.regardingobjecttypecode",
                                @operator: DbArrayFilterOperator.In,
                                fieldValues: new(3, 4, 112, 10912),
                                parameterPrefix: "projectTypeCode"),
                            new DbCombinedFilter(DbLogicalOperator.Or)
                            {
                                Filters =
                                [
                                    new DbRawFilter(
                                        "(p.statecode = 0)"),
                                    new DbRawFilter(
                                        "(t.regardingobjecttypecode = 112 AND EXISTS" +
                                        " (SELECT TOP 1 1 FROM incident AS i WHERE t.regardingobjectid = i.incidentid AND i.statecode = 0))"),
                                    new DbRawFilter(
                                        "(t.regardingobjecttypecode = 4 AND EXISTS" +
                                        " (SELECT TOP 1 1 FROM lead AS l WHERE t.regardingobjectid = l.leadid AND l.statecode = 0))"),
                                    new DbRawFilter(
                                        "(t.regardingobjecttypecode = 3 AND EXISTS" +
                                        " (SELECT TOP 1 1 FROM opportunity AS o WHERE t.regardingobjectid = o.opportunityid AND o.statecode = 0))")
                                ] 
                            }
                        ]
                    },
                    GroupByFields =
                    [
                        "t.regardingobjectid",
                        "t.regardingobjecttypecode"
                    ],
                    Orders =
                    [
                        new("MaxDate", DbOrderType.Descending),
                        new("MaxCreatedOn", DbOrderType.Descending)
                    ]
                }
            }
        };
}