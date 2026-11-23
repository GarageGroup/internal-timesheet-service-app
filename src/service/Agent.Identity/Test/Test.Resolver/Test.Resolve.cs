using GarageGroup.Infra;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Identity.Test;

partial class AgentUserContextResolverTest
{
    [Theory]
    [InlineData(0, 202, 202, AgentUserContextResolveFailureCode.InvalidIdentity)]
    [InlineData(101, 0, 202, AgentUserContextResolveFailureCode.InvalidIdentity)]
    [InlineData(101, 202, 0, AgentUserContextResolveFailureCode.InvalidIdentity)]
    [InlineData(101, 202, -303, AgentUserContextResolveFailureCode.UnsupportedChat)]
    public static async Task ResolveAsync_InvalidIdentity_ExpectFailure(
        long botId,
        long telegramUserId,
        long telegramChatId,
        AgentUserContextResolveFailureCode expectedCode)
    {
        var mockSqlApi = BuildMockSqlApi(default(FlatArray<DbAgentUserBinding>));
        var resolver = new AgentUserContextResolver(mockSqlApi.Object);

        var actual = await resolver.ResolveAsync(
            new(botId, telegramUserId, telegramChatId),
            TestContext.Current.CancellationToken);

        var failure = actual.FailureOrThrow();
        Assert.Equal(expectedCode, failure.FailureCode);
        mockSqlApi.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task ResolveAsync_ValidIdentity_ExpectExpectedQuery()
    {
        var mockSqlApi = BuildMockSqlApi(default(FlatArray<DbAgentUserBinding>));
        var resolver = new AgentUserContextResolver(mockSqlApi.Object);

        _ = await resolver.ResolveAsync(SomeIdentity, TestContext.Current.CancellationToken);

        var expectedQuery = new DbSelectQuery("gg_telegram_bot_user", "b")
        {
            Top = 2,
            SelectedFields =
            [
                "b.gg_telegram_bot_userid AS BindingId",
                "b.gg_systemuser_id AS CrmSystemUserId",
                "b.gg_is_user_signed_out AS IsSignedOut",
                "u.azureactivedirectoryobjectid AS EntraObjectId",
                "u.isdisabled AS IsUserDisabled"
            ],
            JoinedTables =
            [
                new(DbJoinType.Left, "systemuser", "u", "b.gg_systemuser_id = u.systemuserid")
            ],
            Filter = new DbCombinedFilter(DbLogicalOperator.And)
            {
                Filters =
                [
                    new DbRawFilter("b.statecode = 0"),
                    new DbParameterFilter("b.gg_bot_id", DbFilterOperator.Equal, 101L, "botId"),
                    new DbParameterFilter("b.gg_chat_id", DbFilterOperator.Equal, 202L, "telegramUserId")
                ]
            }
        };

        mockSqlApi.Verify(
            a => a.QueryEntitySetOrFailureAsync<DbAgentUserBinding>(expectedQuery, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public static async Task ResolveAsync_BindingNotFound_ExpectUserNotLinkedFailure()
    {
        var mockSqlApi = BuildMockSqlApi(default(FlatArray<DbAgentUserBinding>));
        var resolver = new AgentUserContextResolver(mockSqlApi.Object);

        var actual = await resolver.ResolveAsync(SomeIdentity, TestContext.Current.CancellationToken);

        Assert.Equal(AgentUserContextResolveFailureCode.UserNotLinked, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task ResolveAsync_DbFailure_ExpectUnknownFailureWithSourceException()
    {
        var sourceException = new Exception("SQL is unavailable");
        var sourceFailure = sourceException.ToFailure("SQL query failed");
        var resolver = new AgentUserContextResolver(BuildMockSqlApi(sourceFailure).Object);

        var actual = await resolver.ResolveAsync(SomeIdentity, TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            AgentUserContextResolveFailureCode.Unknown,
            "SQL query failed",
            sourceException);

        Assert.StrictEqual(expected, actual.FailureOrThrow());
    }

    [Fact]
    public static async Task ResolveAsync_SeveralBindings_ExpectAmbiguousBindingFailure()
    {
        FlatArray<DbAgentUserBinding> bindings = [BuildBinding(), BuildBinding()];
        var resolver = new AgentUserContextResolver(BuildMockSqlApi(bindings).Object);

        var actual = await resolver.ResolveAsync(SomeIdentity, TestContext.Current.CancellationToken);

        Assert.Equal(AgentUserContextResolveFailureCode.AmbiguousBinding, actual.FailureOrThrow().FailureCode);
    }

    [Theory]
    [InlineData(true, false, true, AgentUserContextResolveFailureCode.BindingSignedOut)]
    [InlineData(false, true, true, AgentUserContextResolveFailureCode.UserDisabled)]
    [InlineData(false, false, false, AgentUserContextResolveFailureCode.MissingEntraObjectId)]
    public static async Task ResolveAsync_InvalidBinding_ExpectFailure(
        bool isSignedOut,
        bool isUserDisabled,
        bool hasEntraObjectId,
        AgentUserContextResolveFailureCode expectedCode)
    {
        FlatArray<DbAgentUserBinding> bindings =
        [
            BuildBinding() with
            {
                IsSignedOut = isSignedOut,
                IsUserDisabled = isUserDisabled,
                EntraObjectId = hasEntraObjectId ? Guid.NewGuid() : null
            }
        ];

        var resolver = new AgentUserContextResolver(BuildMockSqlApi(bindings).Object);
        var actual = await resolver.ResolveAsync(SomeIdentity, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task ResolveAsync_ValidBinding_ExpectContext()
    {
        var binding = BuildBinding();
        FlatArray<DbAgentUserBinding> bindings = [binding];
        var resolver = new AgentUserContextResolver(BuildMockSqlApi(bindings).Object);

        var actual = await resolver.ResolveAsync(SomeIdentity, TestContext.Current.CancellationToken);

        var expected = new AgentUserContext(
            SomeIdentity.BotId,
            SomeIdentity.TelegramUserId,
            SomeIdentity.TelegramChatId,
            binding.BindingId,
            binding.CrmSystemUserId,
            binding.EntraObjectId.GetValueOrDefault());

        Assert.Equal(expected, actual.SuccessOrThrow());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public static async Task ResolveAsync_EmptyRequiredBindingId_ExpectUnknownFailure(
        bool emptyBindingId,
        bool emptyCrmSystemUserId)
    {
        var binding = BuildBinding() with
        {
            BindingId = emptyBindingId ? Guid.Empty : Guid.NewGuid(),
            CrmSystemUserId = emptyCrmSystemUserId ? Guid.Empty : Guid.NewGuid()
        };

        FlatArray<DbAgentUserBinding> bindings = [binding];
        var resolver = new AgentUserContextResolver(BuildMockSqlApi(bindings).Object);

        var actual = await resolver.ResolveAsync(SomeIdentity, TestContext.Current.CancellationToken);

        Assert.Equal(AgentUserContextResolveFailureCode.Unknown, actual.FailureOrThrow().FailureCode);
    }

}
