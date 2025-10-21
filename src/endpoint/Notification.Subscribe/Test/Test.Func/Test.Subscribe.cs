using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Notification.Subscribe.Test.Test.Func;

partial class NotificationSubscribeFuncTest
{
    [Fact]
    public static async Task SubscribeAsync_BotInfoGetResultIsFailure_ExpectUnknownFailure()
    {
        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var sourceException = new Exception("Some exception");
        var botInfoFailure = sourceException.ToFailure("Some failure");

        var mockBotApi = BuildMockBotApi(botInfoFailure);

        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeSubscribeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(NotificationSubscribeFailureCode.Unknown, "Some failure", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task SubscribeAsync_BotInfoGetResultIsSuccess_ExpectSearchForUserCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var botInfo = new BotInfoGetOut(10, "SomeUserName");
        var mockBotApi = BuildMockBotApi(botInfo);

        var input = new NotificationSubscribeIn(
            Guid.Parse("8235b18a-04e8-4092-985b-c280b5810ff0"), 
            new DailyNotificationSubscriptionData(null));

        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);
        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        var expected = new DataverseEntityGetIn(
            entityPluralName: "gg_telegram_bot_users",
            selectFields: ["gg_telegram_bot_userid"],
            entityKey: new DataverseAlternateKey(
            [
                new("_gg_systemuser_id_value", "8235b18a-04e8-4092-985b-c280b5810ff0"),
                new("gg_bot_id", "'10'"),
            ]));

        mockDataverseApi.Verify(a => a.GetEntityAsync<TelegramBotUserJson>(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, NotificationSubscribeFailureCode.BotUserNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, NotificationSubscribeFailureCode.Unknown)]
    public static async Task SubscribeAsync_FindBotUserResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, NotificationSubscribeFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");

        var mockDataverseApi = BuildMockDataverseApi(
            dataverseFailure, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeSubscribeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task SubscribeAsync_ExpectFindDailyNotificationTypeCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var input = new NotificationSubscribeIn(
            Guid.Parse("8235b18a-04e8-4092-985b-c280b5810ff0"), 
            new DailyNotificationSubscriptionData(null));

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        var expected = new DataverseEntityGetIn(
            entityPluralName: "gg_bot_notification_types",
            selectFields: ["gg_bot_notification_typeid"],
            entityKey: new DataverseAlternateKey("gg_key", "'dailyTimesheetNotification'"));

        mockDataverseApi.Verify(a => a.GetEntityAsync<NotificationTypeJson>(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task SubscribeAsync_ExpectFindWeeklyNotificationTypeCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);
 
        var input = new NotificationSubscribeIn(
            Guid.Parse("8235b18a-04e8-4092-985b-c280b5810ff0"), 
            new WeeklyNotificationSubscriptionData(null));

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        var expected = new DataverseEntityGetIn(
            entityPluralName: "gg_bot_notification_types",
            selectFields: ["gg_bot_notification_typeid"],
            entityKey: new DataverseAlternateKey("gg_key", "'weeklyTimesheetNotification'"));

        mockDataverseApi.Verify(a => a.GetEntityAsync<NotificationTypeJson>(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, NotificationSubscribeFailureCode.NotificationTypeNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, NotificationSubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, NotificationSubscribeFailureCode.Unknown)]
    public static async Task SubscribeAsync_FindNotificationTypeResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, NotificationSubscribeFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");

        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, dataverseFailure, Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeSubscribeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(NotificationSubscribeFuncSource.InputInvalidTestData), MemberType = typeof(NotificationSubscribeFuncSource))]
    public static async Task SubscribeAsync_UserPreferenceIsInvalid_ExpectValidationFailure(
        NotificationSubscribeIn input, Failure<NotificationSubscribeFailureCode> expected)
    {
        var mockDataverseApi = BuildMockDataverseApi(
            botUserGetResult: SomeDataverseTelegramBotUser,
            notificationTypeGetResult:SomeDataverseNotificationType,
            subscriptionUpdateResult: Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(NotificationSubscribeFuncSource.InputValidTestData), MemberType = typeof(NotificationSubscribeFuncSource))]
    internal static async Task SubscribeAsync_InputIsValid_ExpectUpsertNotificationSubscriptionOnce(
        NotificationSubscribeIn input, NotificationSubscriptionJson expectedSubscriptionJson)
    {
        var botUser = new TelegramBotUserJson
        {
            Id = Guid.Parse("1c89a21f-533f-41db-9855-9839e5685bad")
        };

        var notificationType = new NotificationTypeJson
        {
            Id = Guid.Parse("9e7abfe2-12be-435b-bc40-795ce1a4212f")
        };

        var mockDataverseApi = BuildMockDataverseApi(
            botUserGetResult: new DataverseEntityGetOut<TelegramBotUserJson>(botUser),
            notificationTypeGetResult: new DataverseEntityGetOut<NotificationTypeJson>(notificationType),
            subscriptionUpdateResult: Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        var expected = new DataverseEntityUpdateIn<NotificationSubscriptionJson>(
            entityPluralName: "gg_bot_user_subscriptions",
            entityKey: new DataverseAlternateKey(
                [
                    new ("_gg_bot_user_id_value", "1c89a21f-533f-41db-9855-9839e5685bad"),
                    new ("_gg_notification_type_id_value", "9e7abfe2-12be-435b-bc40-795ce1a4212f")
                ]),
            entityData: expectedSubscriptionJson)
        {
            OperationType = DataverseUpdateOperationType.Upsert
        };

        mockDataverseApi.Verify(a => a.UpdateEntityAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized)]
    [InlineData(DataverseFailureCode.RecordNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange)]
    [InlineData(DataverseFailureCode.UserNotEnabled)]
    [InlineData(DataverseFailureCode.PrivilegeDenied)]
    [InlineData(DataverseFailureCode.Throttling)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound)]
    [InlineData(DataverseFailureCode.DuplicateRecord)]
    [InlineData(DataverseFailureCode.InvalidPayload)]
    [InlineData(DataverseFailureCode.InvalidFileSize)]
    public static async Task SubscribeAsync_NotificationUpsertIsFailure_ExpectUnknownFailure(
        DataverseFailureCode dataverseFailureCode)
    {
        var sourceException = new Exception("Some exception");
        var dataverseFailure = sourceException.ToFailure(dataverseFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(SomeDataverseTelegramBotUser, SomeDataverseNotificationType, dataverseFailure);
        var mockBotApi = BuildMockBotApi(SomeBotInfo);

        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeSubscribeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(NotificationSubscribeFailureCode.Unknown, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task SubscribeAsync_AllResultsAreSuccesses_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeSubscribeInput, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}
