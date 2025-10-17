using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.BotApi.Test;

partial class BotApiTest
{
    [Fact]
    public static async Task PingAsync_ExpectBotUserMeCalledOnce()
    {
        var mockTelegramApi = BuildMockTelegramApi(SomeBotUser);
        var mockCacheApi = BuildMockCacheApi(SomeCacheValue);

        var api = new BotApiImpl(mockTelegramApi.Object, mockCacheApi.Object);
        _ = await api.PingAsync(default, TestContext.Current.CancellationToken);

        mockTelegramApi.Verify(a => a.GetMeAsync(default, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(TelegramBotFailureCode.Unknown)]
    [InlineData(TelegramBotFailureCode.BadRequest)]
    [InlineData(TelegramBotFailureCode.Unauthorized)]
    [InlineData(TelegramBotFailureCode.Forbidden)]
    [InlineData(TelegramBotFailureCode.Conflict)]
    [InlineData(TelegramBotFailureCode.TooManyRequests)]
    public static async Task PingAsync_BotUserMeResultIsFailure_ExpectFailure(
        TelegramBotFailureCode botFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var botFailure = sourceException.ToFailure(botFailureCode, "Some failure message");

        var mockTelegramApi = BuildMockTelegramApi(botFailure);
        var mockCacheApi = BuildMockCacheApi(SomeCacheValue);

        var api = new BotApiImpl(mockTelegramApi.Object, mockCacheApi.Object);

        var actual = await api.PingAsync(default, TestContext.Current.CancellationToken);
        var expected = Failure.Create("Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(BotApiSource.CacheSetTestData), MemberType = typeof(BotApiSource))]
    internal static async Task PingAsync_BotUserMeResultIsSuccess_ExpectCacheSetCalledOnce(
        BotUser botUser, CacheValue cacheValue)
    {
        var mockTelegramApi = BuildMockTelegramApi(botUser);
        var mockCacheApi = BuildMockCacheApi(SomeCacheValue);

        var api = new BotApiImpl(mockTelegramApi.Object, mockCacheApi.Object);
        _ = await api.PingAsync(default, TestContext.Current.CancellationToken);

        mockCacheApi.Verify(a => a.SetValue(cacheValue), Times.Once);
    }

    [Fact]
    public static async Task PingAsync_BotUserMeResultIsSuccess_ExpectSuccess()
    {
        var mockTelegramApi = BuildMockTelegramApi(SomeBotUser);
        var mockCacheApi = BuildMockCacheApi(SomeCacheValue);

        var api = new BotApiImpl(mockTelegramApi.Object, mockCacheApi.Object);

        var actual = await api.PingAsync(default, TestContext.Current.CancellationToken);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}