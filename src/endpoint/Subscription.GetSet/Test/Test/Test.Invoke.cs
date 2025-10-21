using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Subscription.GetSet.Test;

partial class SubscriptionSetGetFuncTest
{
    [Fact]
    public static async Task InvokeAsync_BotInfoGetResultIsFailure_ExpectUnknownFailure()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeDataverseSubscriptionsOut);

        var sourceException = new Exception("Some exception");
        var botInfoFailure = sourceException.ToFailure("Some failure");

        var mockBotApi = BuildMockBotApi(botInfoFailure);
        var func = new SubscriptionSetGetFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(SubscriptionSetGetFailureCode.Unknown, "Some failure", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task InvokeAsync_BotInfoGetResultIsSuccess_ExpectDataverseGetSetCalledOnce()
    {
        var mockDataverseApi = BuildMockDataverseApi(SomeDataverseSubscriptionsOut);

        var botInfo = new BotInfoGetOut(5674427344, "SomeName");
        var mockBotApi = BuildMockBotApi(botInfo);

        var func = new SubscriptionSetGetFunc(mockDataverseApi.Object, mockBotApi.Object);

        var input = new SubscriptionSetGetIn(
            systemUserId: new("5a0aa34a-b64a-4258-bed3-5e62946db8b4"));

        _ = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        var expectedInput = new DataverseEntitySetGetIn(
            entityPluralName: "gg_bot_user_subscriptions",
            selectFields: ["gg_notification_preferences"],
            expandFields: [new("gg_notification_type_id", ["gg_key"])],
            filter:
                "(gg_bot_user_id%2f_gg_systemuser_id_value eq '5a0aa34a-b64a-4258-bed3-5e62946db8b4'" +
                " and gg_bot_user_id%2fgg_bot_id eq '5674427344' and gg_is_disabled ne true)");

        mockDataverseApi.Verify(x => x.GetEntitySetAsync<SubscriptionJson>(expectedInput, It.IsAny<CancellationToken>()), Times.Once);
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
    public static async Task InvokeAsync_DataverseGetSetResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");

        var mockDataverseApi = BuildMockDataverseApi(dataverseFailure);
        var mockBotApi = BuildMockBotApi(SomeBotInfo);

        var func = new SubscriptionSetGetFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
        var expected = Failure.Create(SubscriptionSetGetFailureCode.Unknown, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(SubscriptionSetGetFuncSource.OutputInvalidTestData), MemberType = typeof(SubscriptionSetGetFuncSource))]
    internal static async Task InvokeAsync_DataverseGetSetResultNotificationTypeIsInvalid_ExpectInvalidOperationException(
        FlatArray<SubscriptionJson> jsonSubscriptions, string expectedErrorMessage)
    {
        var dataverseSubscriptionsOut = new DataverseEntitySetGetOut<SubscriptionJson>(jsonSubscriptions);
        var mockDataverseApi = BuildMockDataverseApi(dataverseSubscriptionsOut);

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new SubscriptionSetGetFunc(mockDataverseApi.Object, mockBotApi.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(TestAsync);
        Assert.Equal(expectedErrorMessage, ex.Message);

        async Task TestAsync()
            =>
            await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);
    }

    [Theory]
    [MemberData(nameof(SubscriptionSetGetFuncSource.OutputValidTestData), MemberType = typeof(SubscriptionSetGetFuncSource))]
    internal static async Task InvokeAsync_DataverseGetSetResultIsSuccess_ExpectSuccess(
        FlatArray<SubscriptionJson> jsonSubscriptions, SubscriptionSetGetOut expected)
    {
        var dataverseSubscriptionsOut = new DataverseEntitySetGetOut<SubscriptionJson>(jsonSubscriptions);
        var mockDataverseApi = BuildMockDataverseApi(dataverseSubscriptionsOut);

        var mockBotApi = BuildMockBotApi(SomeBotInfo);
        var func = new SubscriptionSetGetFunc(mockDataverseApi.Object, mockBotApi.Object);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        Assert.StrictEqual(expected, actual);
    }
}