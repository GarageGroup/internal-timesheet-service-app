using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.User.SignIn.Test;

partial class UserSignInFuncTest
{
    [Theory]
    [MemberData(nameof(UserSignInFuncSource.InputInvalidTestData), MemberType = typeof(UserSignInFuncSource))]
    internal static async Task InvokeAsync_InvalidTelegramData_ExpectFailure(
        UserSignInIn input, Failure<UserSignInFailureCode> expectedFailure)
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeSystemUserResult, Result.Success<Unit>(default));
        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var mockValidator = BuildMockTelegramDataValidator(expectedFailure);

        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);
        var actual = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        Assert.StrictEqual(expectedFailure, actual);
    }

    [Fact]
    public static async Task InvokeAsync_InputIsValid_ExpectDataverseGetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeSystemUserResult, Result.Success<Unit>(default));
        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var mockValidator = BuildMockTelegramDataValidator(123123L);

        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);

        var input = new UserSignInIn(
            systemUserId: new("3f414904-3128-4f27-af56-d5f45bf31dd5"),
            telegramData: "query_id=AAGmGqACAASCAKYaoAKgWTfQ&user=%7B%22id%22%3A123123%2C%22" +
                "first_name%22%3A%22test%22%2C%22last_name%22%3A%22%22%2C%22username%22%3A%22TEST%22%2C%22" +
                "language_code%22%3A%22en%22%2C%22allows_write_to_pm%22%3Atrue%7D&auth_date=1720097842&" +
                "hash=2fa9c34a28f2a843eca1a086262000e6d0bda91db3a8ddf4002ca5bd26a5c224");

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        var expectedInput = new DataverseEntityGetIn(
            entityPluralName: "systemusers",
            entityKey: new DataversePrimaryKey(new("3f414904-3128-4f27-af56-d5f45bf31dd5")),
            selectFields: ["yomifullname"]);

        mockDataverseApi.Verify(a => a.GetEntityAsync<SystemUserJson>(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, UserSignInFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, UserSignInFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, UserSignInFailureCode.SystemUserNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, UserSignInFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, UserSignInFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, UserSignInFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, UserSignInFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, UserSignInFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, UserSignInFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, UserSignInFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, UserSignInFailureCode.Unknown)]
    public static async Task InvokeAsync_DataverseGetResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, UserSignInFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(dataverseFailure, Result.Success<Unit>(default));
        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var mockValidator = BuildMockTelegramDataValidator(123123L);

        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_BotInfoGetResultIsFailure_ExpectUnknownFailure()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeSystemUserResult, Result.Success<Unit>(default));

        var sourceException = new Exception("Some exception");
        var botInfoFailure = sourceException.ToFailure("Some failure text");

        var mockBotApi = BuildMockBotApi(botInfoFailure);
        var mockValidator = BuildMockTelegramDataValidator(123123L);
        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(UserSignInFailureCode.Unknown, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_UserIsValid_ExpectBindingQueryCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeSystemUserResult, Result.Success<Unit>(default));
        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var mockValidator = BuildMockTelegramDataValidator(123123L);
        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);

        _ = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expected = new DataverseEntitySetGetIn(
            entityPluralName: "gg_telegram_bot_users",
            selectFields: ["_gg_systemuser_id_value"],
            filter: "(gg_bot_id eq '79237382' and gg_chat_id eq '123123' and statecode eq 0)");

        mockDataverseApi.Verify(
            a => a.GetEntitySetAsync<UserBindingJson>(expected, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_TelegramUserLinkedToAnotherUser_ExpectConflictAndNoUpdate()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeSystemUserResult, Result.Success<Unit>(default));
        SetupBindings(
            mockDataverseApi,
            new DataverseEntitySetGetOut<UserBindingJson>(
                [new() { CrmSystemUserId = Guid.Parse("4d414737-c120-46dd-b7d4-14db80112553") }]));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var mockValidator = BuildMockTelegramDataValidator(123123L);
        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            UserSignInFailureCode.TelegramUserAlreadyLinked,
            "Telegram user is already linked to another system user");

        Assert.StrictEqual(expected, actual);
        mockDataverseApi.Verify(
            static a => a.UpdateEntityAsync(It.IsAny<DataverseEntityUpdateIn<UserJson>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public static async Task InvokeAsync_TelegramUserLinkedToSameUser_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeSystemUserResult, Result.Success<Unit>(default));
        SetupBindings(
            mockDataverseApi,
            new DataverseEntitySetGetOut<UserBindingJson>(
                [new() { CrmSystemUserId = SomeInput.SystemUserId }]));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var mockValidator = BuildMockTelegramDataValidator(123123L);
        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        Assert.True(actual.IsSuccess);
    }

    [Fact]
    public static async Task InvokeAsync_SeveralTelegramBindings_ExpectConflict()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeSystemUserResult, Result.Success<Unit>(default));
        SetupBindings(
            mockDataverseApi,
            new DataverseEntitySetGetOut<UserBindingJson>(
                [
                    new() { CrmSystemUserId = SomeInput.SystemUserId },
                    new() { CrmSystemUserId = SomeInput.SystemUserId }
                ]));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var mockValidator = BuildMockTelegramDataValidator(123123L);
        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            UserSignInFailureCode.TelegramUserAlreadyLinked,
            "Several active Telegram user bindings were found");

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_BindingQueryFailed_ExpectUnknownFailure()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeSystemUserResult, Result.Success<Unit>(default));
        var sourceException = new Exception("Dataverse is unavailable");
        SetupBindings(mockDataverseApi, sourceException.ToFailure(DataverseFailureCode.Throttling, "Query failed"));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var mockValidator = BuildMockTelegramDataValidator(123123L);
        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        var expected = Failure.Create(UserSignInFailureCode.Unknown, "Query failed", sourceException);
        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(UserSignInFuncSource.InputTestData), MemberType = typeof(UserSignInFuncSource))]
    internal static async Task InvokeAsync_DataverseGetResultIsSuccess_ExpectUpdateDataverseUpdateCalledOnce(
        BotInfoGetOut botInfo,
        UserSignInIn input,
        DataverseEntityGetOut<SystemUserJson> systemUserResult,
        DataverseEntityUpdateIn<UserJson> expectedInput)
    {
        var mockDataverseApi = BuildMockDataverseApi(systemUserResult, Result.Success<Unit>(default));
        var mockBotApi = BuildMockBotApi(botInfo);
        var mockValidator = BuildMockTelegramDataValidator(123123L);

        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);
        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        mockDataverseApi.Verify(a => a.UpdateEntityAsync(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized)]
    [InlineData(DataverseFailureCode.RecordNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange)]
    [InlineData(DataverseFailureCode.Throttling)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound)]
    [InlineData(DataverseFailureCode.DuplicateRecord)]
    [InlineData(DataverseFailureCode.InvalidPayload)]
    [InlineData(DataverseFailureCode.UserNotEnabled)]
    [InlineData(DataverseFailureCode.PrivilegeDenied)]
    [InlineData(DataverseFailureCode.InvalidFileSize)]
    public static async Task InvokeAsync_DataverseUpdateResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(SomeSystemUserResult, dataverseFailure);
        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var mockValidator = BuildMockTelegramDataValidator(123123L);

        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(UserSignInFailureCode.Unknown, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeSystemUserResult, Result.Success<Unit>(default));
        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var mockValidator = BuildMockTelegramDataValidator(123123L);

        var func = new UserSignInFunc(mockDataverseApi.Object, mockBotApi.Object, mockValidator.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}
