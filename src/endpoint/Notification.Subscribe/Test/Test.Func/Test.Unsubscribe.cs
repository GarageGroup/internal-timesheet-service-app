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
    public static async Task UnsubscribeAsync_BotInfoGetResultIsFailure_ExpectUnknownFailure()
    {
        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var sourceException = new Exception("Some error");
        var botInfoFailure = sourceException.ToFailure("Some failure message");

        var mockBotApi = BuildMockBotApi(botInfoFailure);

        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeUnsubscribeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(NotificationUnsubscribeFailureCode.Unknown, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task UnsubscribeAsync_BotInfoGetResultIsSuccess_ExpectSearchForUserCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var botInfo = new BotInfoGetOut(10, "SomeName");
        var mockBotApi = BuildMockBotApi(botInfo);

        var input = new NotificationUnsubscribeIn(
            Guid.Parse("8235b18a-04e8-4092-985b-c280b5810ff0"),
            NotificationType.DailyNotification);

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

        mockDataverseApi.Verify(x => x.GetEntityAsync<TelegramBotUserJson>(expected, It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Theory]
    [InlineData(DataverseFailureCode.Unknown, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, NotificationUnsubscribeFailureCode.BotUserNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, NotificationUnsubscribeFailureCode.Unknown)]
    public static async Task UnsubscribeAsync_FindBotUserResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, NotificationUnsubscribeFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");

        var mockDataverseApi = BuildMockDataverseApi(
            dataverseFailure, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeUnsubscribeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [InlineData(NotificationType.DailyNotification, "dailyTimesheetNotification")]
    [InlineData(NotificationType.WeeklyNotification, "weeklyTimesheetNotification")]
    public static async Task UnsubscribeAsync_ExpectFindNotificationTypeCalledOnce(
        NotificationType notificationType, string notificationTypeKey)
    {
        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var input = new NotificationUnsubscribeIn(
            Guid.Parse("8235b18a-04e8-4092-985b-c280b5810ff0"),
            notificationType);

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);
    
        var expected = new DataverseEntityGetIn(
            entityPluralName: "gg_bot_notification_types",
            selectFields: ["gg_bot_notification_typeid"],
            entityKey: new DataverseAlternateKey("gg_key", $"'{notificationTypeKey}'"));
    
        mockDataverseApi.Verify(x => x.GetEntityAsync<NotificationTypeJson>(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, NotificationUnsubscribeFailureCode.NotificationTypeNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, NotificationUnsubscribeFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, NotificationUnsubscribeFailureCode.Unknown)]
    public static async Task UnsubscribeAsync_FindNotificationTypeResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, NotificationUnsubscribeFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");

        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, dataverseFailure, Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);
    
        var actual = await func.InvokeAsync(SomeUnsubscribeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }
  
    [Theory]
    [InlineData(NotificationType.DailyNotification)]
    [InlineData(NotificationType.WeeklyNotification)]
    internal static async Task UnsubscribeAsync_ExpectUpdateNotificationUnsubscriptionOnce(NotificationType type)
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

        var input = new NotificationUnsubscribeIn(
            Guid.Parse("1c89a21f-533f-41db-9855-9839e5685bad"),
            type);

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        var expectedSubscriptionJson = new NotificationSubscriptionJson
        {
            IsDisabled = true
        };

        var expected = new DataverseEntityUpdateIn<NotificationSubscriptionJson>(
            entityPluralName: "gg_bot_user_subscriptions",
            entityKey: new DataverseAlternateKey(
                [
                    new ("_gg_bot_user_id_value", "1c89a21f-533f-41db-9855-9839e5685bad"),
                    new ("_gg_notification_type_id_value", "9e7abfe2-12be-435b-bc40-795ce1a4212f")
                ]),
            entityData: expectedSubscriptionJson)
        {
            OperationType = DataverseUpdateOperationType.Update
        };

        mockDataverseApi.Verify(x => x.UpdateEntityAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange)]
    [InlineData(DataverseFailureCode.UserNotEnabled)]
    [InlineData(DataverseFailureCode.PrivilegeDenied)]
    [InlineData(DataverseFailureCode.Throttling)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound)]
    [InlineData(DataverseFailureCode.DuplicateRecord)]
    [InlineData(DataverseFailureCode.InvalidPayload)]
    [InlineData(DataverseFailureCode.InvalidFileSize)]
    public static async Task UnsubscribeAsync_NotificationUpdateIsFailure_ExpectUnknownFailure(
        DataverseFailureCode dataverseFailureCode)
    {
        var sourceException = new Exception("Some exception");
        var dataverseFailure = sourceException.ToFailure(dataverseFailureCode, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, dataverseFailure);

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeUnsubscribeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(NotificationUnsubscribeFailureCode.Unknown, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }
   
    [Fact]
    public static async Task UnsubscribeAsync_DataverseReturnsSubscriptionNotFound_ExpectSuccessResult()
    {
        var sourceException = new Exception("Some exception");
        var dataverseFailure = sourceException.ToFailure(DataverseFailureCode.RecordNotFound, "Some failure message");

        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, dataverseFailure);

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeUnsubscribeInput, TestContext.Current.CancellationToken);

        Assert.StrictEqual(Unit.Value, actual);
    }

    [Fact]
    public static async Task UnsubscribeAsync_AllResultsAreSuccesses_ExpectSuccess()
    {
        var mockDataverseApi = BuildMockDataverseApi(
            SomeDataverseTelegramBotUser, SomeDataverseNotificationType, Result.Success<Unit>(default));

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new NotificationSubscribeFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeUnsubscribeInput, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}