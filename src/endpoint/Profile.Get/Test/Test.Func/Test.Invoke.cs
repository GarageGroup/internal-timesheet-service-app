using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Profile.Get.Test;

partial class ProfileGetFuncTest
{
    [Fact]
    public static async Task InvokeAsync_BotInfoGetResultIsFailure_ExpectUnknownFailure()
    {
        var mockSqlApi = BuildMockSqlApi(SomeDbProfile);

        var sourceException = new Exception("Some error message");
        var botInfoFailure = sourceException.ToFailure("Some Failure");

        var mockBotApi = BuildMockBotApi(botInfoFailure);
        var func = new ProfileGetFunc(mockSqlApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(ProfileGetFailureCode.Unknown, "Some Failure", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_BotInfoGetResultIsSuccess_ExpectMockSqlApiCalledOnce()
    {
        var mockSqlApi = BuildMockSqlApi(SomeDbProfile);

        const long botId = 123123;
        var botInfo = new BotInfoGetOut(botId, "SomeName");

        var mockBotApi = BuildMockBotApi(botInfo);
        var func = new ProfileGetFunc(mockSqlApi.Object, mockBotApi.Object);

        var input = new ProfileGetIn(
            systemUserId: new("bef33be0-99f5-4018-ba80-3366ec9ec1fd"));

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        var expectedQuery = new DbSelectQuery("gg_telegram_bot_user", "p")
        {
            Top = 1,
            SelectedFields =
            [
                "p.gg_systemuser_idname AS UserName",
                "p.gg_language_code AS LanguageCode"
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
                                    new DbRawFilter("p.gg_systemuser_id = u.systemuserid"),
                                    new DbParameterFilter(
                                        fieldName: "u.azureactivedirectoryobjectid",
                                        @operator: DbFilterOperator.Equal,
                                        fieldValue: Guid.Parse("bef33be0-99f5-4018-ba80-3366ec9ec1fd"),
                                        parameterName: "systemUserId")
                                ]
                            }
                        }),
                    new DbParameterFilter(
                        fieldName: "p.gg_bot_id",
                        @operator: DbFilterOperator.Equal,
                        fieldValue: botId,
                        parameterName: "botId")
                ]
            }
        };

        mockSqlApi.Verify(a => a.QueryEntityOrFailureAsync<DbProfile>(expectedQuery, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(EntityQueryFailureCode.Unknown, ProfileGetFailureCode.Unknown)]
    [InlineData(EntityQueryFailureCode.NotFound, ProfileGetFailureCode.NotFound)]
    public static async Task InvokeAsync_DbResultIsFailure_ExpectFailure(
        EntityQueryFailureCode sourceFailureCode, ProfileGetFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dbFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockSqlApi = BuildMockSqlApi(dbFailure);
        var mockBotApi = BuildMockBotApi(SomeBotInfo);

        var func = new ProfileGetFunc(mockSqlApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_DbResultIsSuccess_ExpectSuccess()
    {
        var dbProfile = new DbProfile
        {
            UserName = "test",
            LanguageCode = "ru"
        };

        var mockSqlApi = BuildMockSqlApi(dbProfile);
        var mockBotApi = BuildMockBotApi(SomeBotInfo);

        var func = new ProfileGetFunc(mockSqlApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expected = new ProfileGetOut(
            userName: "test",
            languageCode: "ru");

        Assert.StrictEqual(expected, actual);
    }
}
