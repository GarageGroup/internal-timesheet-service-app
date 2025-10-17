using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Tag.GetSet.Test;

partial class TagSetGetFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ExpectSqlApiCalledOnce()
    {
        var mockSqlApi = BuildMockSqlApi(SomeDbTimesheetTagSet);

        var date = new DateOnly(2024, 03, 21);
        var todayProvider = BuildTodayProvider(date);

        var option = new TagSetGetOption(5);
        var func = new TagSetGetFunc(mockSqlApi.Object, todayProvider, option);

        var input = new TagSetGetIn(
            systemUserId: new("82ee3d26-17f1-4e2f-adb2-eeea5119a512"),
            projectId: new("58482d23-ca3e-4499-8294-cc9b588cce73"));

        var cancellationToken = new CancellationToken(false);
        _ = await func.InvokeAsync(input, cancellationToken);

        var expectedQuery = new DbSelectQuery("gg_timesheetactivity", "t")
        {
            SelectedFields = new("t.gg_description AS Description"),
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
                                        fieldValue: Guid.Parse("82ee3d26-17f1-4e2f-adb2-eeea5119a512"),
                                        parameterName: "ownerId")
                                ]
                            }
                        }),
                    new DbParameterFilter(
                        "t.regardingobjectid", DbFilterOperator.Equal, Guid.Parse("58482d23-ca3e-4499-8294-cc9b588cce73"), "projectId"),
                    new DbLikeFilter(
                        "t.gg_description", "%#%", "description"),
                    new DbParameterFilter(
                        "t.gg_date", DbFilterOperator.GreaterOrEqual, "2024-03-16", "minDate"),
                    new DbParameterFilter(
                        "t.gg_date", DbFilterOperator.LessOrEqual, "2024-03-21", "maxDate")
                ]
            },
            Orders =
            [
                new("t.gg_date", DbOrderType.Descending),
                new("t.createdon", DbOrderType.Descending)
            ]
        };

        mockSqlApi.Verify(a => a.QueryEntitySetOrFailureAsync<DbTag>(expectedQuery, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_DbResultIsFailure_ExpectFailure()
    {
        var sourceException = new Exception("Some error message");
        var dbFailure = sourceException.ToFailure("Some failure text");

        var mockSqlApi = BuildMockSqlApi(dbFailure);
        var todayProvider = BuildTodayProvider(SomeDate);

        var func = new TagSetGetFunc(mockSqlApi.Object, todayProvider, SomeOption);

        var actual = await func.InvokeAsync(SomeTimesheetTagSetGetInput, default);
        var expected = Failure.Create("Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TagSetGetFuncSource.OutputTestData), MemberType = typeof(TagSetGetFuncSource))]
    internal static async Task InvokeAsync_DbResultIsSuccess_ExpectSuccess(
        FlatArray<DbTag> dbTimesheetTags, TagSetGetOut expected)
    {
        var mockSqlApi = BuildMockSqlApi(dbTimesheetTags);
        var todayProvider = BuildTodayProvider(SomeDate);

        var func = new TagSetGetFunc(mockSqlApi.Object, todayProvider, SomeOption);

        var actual = await func.InvokeAsync(SomeTimesheetTagSetGetInput, default);
        Assert.StrictEqual(expected, actual);
    }
}