using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Timesheet.GetSet.Test;

partial class TimesheetSetGetFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ExpectSqlApiCalledOnce()
    {
        var mockSqlApi = BuildMockSqlApi(SomeDbTimesheetSet);
        var func = new TimesheetSetGetFunc(mockSqlApi.Object);

        var input = new TimesheetSetGetIn(
            systemUserId: new("bd8b8e33-554e-e611-80dc-c4346bad0190"),
            dateFrom: new(2022, 03, 05),
            dateTo: new(2022, 02, 05));

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        var expectedQuery = new DbSelectQuery("gg_timesheetactivity", "t")
        {
            SelectedFields =
            [
                "t.gg_duration AS Duration",
                "t.regardingobjectid AS ProjectId",
                "t.regardingobjecttypecode AS ProjectTypeCode",
                "t.regardingobjectidname AS ProjectName",
                "p.gg_comment AS ProjectComment",
                "(SELECT TOP 1 sub.subject " +
                "FROM gg_timesheetactivity sub " +
                "WHERE sub.regardingobjectid = t.regardingobjectid " +
                "ORDER BY sub.createdon DESC) AS Subject",
                "t.gg_description AS Description",
                "t.activityid AS Id",
                "t.statecode AS TimesheetStateCode",
                "t.gg_date AS Date"
            ],
            JoinedTables =
            [
                new(DbJoinType.Left,  "gg_project", "p", "t.regardingobjecttypecode = 10912 AND t.regardingobjectid = p.gg_projectid")
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
                                        fieldValue: Guid.Parse("bd8b8e33-554e-e611-80dc-c4346bad0190"),
                                        parameterName: "ownerId")
                                ]
                            }
                        }),
                    new DbCombinedFilter(DbLogicalOperator.And)
                    {
                        Filters =
                        [
                            new DbParameterFilter("t.gg_date", DbFilterOperator.GreaterOrEqual, "2022-03-05", "dateFrom"),
                            new DbParameterFilter("t.gg_date", DbFilterOperator.LessOrEqual, "2022-02-05", "dateTo"),
                        ]
                    },                    
                    new DbParameterArrayFilter(
                        fieldName: "t.regardingobjecttypecode",
                        @operator: DbArrayFilterOperator.In,
                        fieldValues: new(3, 4, 112, 10912),
                        parameterPrefix: "projectTypeCode")
                ]
            },
            Orders =
            [
                new("t.gg_date", DbOrderType.Descending),
                new("t.createdon", DbOrderType.Ascending)
            ]
        };

        mockSqlApi.Verify(a => a.QueryEntitySetOrFailureAsync<DbTimesheet>(expectedQuery, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_DbResultIsFailure_ExpectFailure()
    {
        var sourceException = new Exception("Some error message");
        var dbFailure = sourceException.ToFailure("Some failure message");

        var mockSqlApi = BuildMockSqlApi(dbFailure);
        var func = new TimesheetSetGetFunc(mockSqlApi.Object);

        var actual = await func.InvokeAsync(SomeTimesheetSetGetInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(Unit.Value, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TimesheetSetGetFuncSource.OutputGetTestData), MemberType = typeof(TimesheetSetGetFuncSource))]
    internal static async Task InvokeAsync_DataverseResultIsSuccess_ExpectSuccess(
        FlatArray<DbTimesheet> dbTimesheets, TimesheetSetGetOut expected)
    {
        var mockSqlApi = BuildMockSqlApi(dbTimesheets);
        var func = new TimesheetSetGetFunc(mockSqlApi.Object);

        var actual = await func.InvokeAsync(SomeTimesheetSetGetInput, TestContext.Current.CancellationToken);
        Assert.StrictEqual(expected, actual);
    }
}