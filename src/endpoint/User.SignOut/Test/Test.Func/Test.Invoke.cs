using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.User.SignOut.Test;

partial class UserSignOutFuncTest
{
    [Fact]
    public static async Task InvokeAsync_BotInfoGetResultIsFailure_ExpectFailure()
    {
        var mockDataverseApi = BuildMockDataverseApi(Result.Success<Unit>(default));

        var sourceException = new Exception("Some Exception");
        var botInfoFailure = sourceException.ToFailure("Some error message");

        var mockBotApi = BuildMockBotApi(botInfoFailure);
        var func = new UserSignOutFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create("Some error message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(UserSignOutFuncSource.InputTestData), MemberType = typeof(UserSignOutFuncSource))]
    internal static async Task InvokeAsync_BotInfoGetResultIsSuccess_ExpectDataverseUpdateCalledOnce(
        BotInfoGetOut botInfo, UserSignOutIn input, DataverseEntityUpdateIn<UserJson> expectedInput)
    {
        var mockDataverseApi = BuildMockDataverseApi(Result.Success<Unit>(default));
        var mockBotApi = BuildMockBotApi(botInfo);

        var func = new UserSignOutFunc(mockDataverseApi.Object, mockBotApi.Object);
        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(a => a.UpdateEntityAsync(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange)]
    [InlineData(DataverseFailureCode.Throttling)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound)]
    [InlineData(DataverseFailureCode.DuplicateRecord)]
    [InlineData(DataverseFailureCode.InvalidPayload)]
    [InlineData(DataverseFailureCode.UserNotEnabled)]
    [InlineData(DataverseFailureCode.PrivilegeDenied)]
    [InlineData(DataverseFailureCode.InvalidFileSize)]
    public static async Task InvokeAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(dataverseFailure);
        var mockBotApi = BuildMockBotApi(SomeBotInfo);

        var func = new UserSignOutFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create("Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_DataverseResultIsRecordNotFoundFailure_ExpectSuccess()
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(DataverseFailureCode.RecordNotFound, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(dataverseFailure);
        var mockBotApi = BuildMockBotApi(SomeBotInfo);

        var func = new UserSignOutFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseApi(Result.Success<Unit>(default));
        var mockBotApi = BuildMockBotApi(SomeBotInfo);

        var func = new UserSignOutFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}
