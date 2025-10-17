using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Profile.Update.Test.Test;

partial class ProfileUpdateFuncTest
{
    [Fact]
    public static async Task InvokeAsync_BotInfoGetResultIsFailure_ExpectUnknownFailure()
    {
        var mockDataverseApi = BuildMockDataverseApi(Result.Success<Unit>(default));

        var sourceException = new Exception("Some error");
        var botInfoFailure = sourceException.ToFailure("Some failure message");

        var mockBotApi = BuildMockBotApi(botInfoFailure);
        var func = new ProfileUpdateFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(ProfileUpdateFailureCode.Unknown, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ProfileUpdateFuncSource.InputTestData), MemberType = typeof(ProfileUpdateFuncSource))]
    internal static async Task InvokeAsync_BotInfoGetResultIsSuccess_ExpectDataverseUpdateCalledOnce(
        BotInfoGetOut botInfo, ProfileUpdateIn input, DataverseEntityUpdateIn<ProfileJson> expectedInput)
    {
        var mockDataverseApi = BuildMockDataverseApi(Result.Success<Unit>(default));
        var mockBotApi = BuildMockBotApi(botInfo);

        var func = new ProfileUpdateFunc(mockDataverseApi.Object, mockBotApi.Object);
        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(a => a.UpdateEntityAsync(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, ProfileUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, ProfileUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, ProfileUpdateFailureCode.NotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, ProfileUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, ProfileUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, ProfileUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, ProfileUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, ProfileUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, ProfileUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, ProfileUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, ProfileUpdateFailureCode.Unknown)]
    public static async Task InvokeAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, ProfileUpdateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(dataverseFailure);
        var mockBotApi = BuildMockBotApi(SomeBotInfo);

        var func = new ProfileUpdateFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseApi(Result.Success<Unit>(default));
        var mockBotApi = BuildMockBotApi(SomeBotInfo);

        var func = new ProfileUpdateFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}