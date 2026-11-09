using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test;

partial class AgentConversationTableApiTest
{
    [Fact]
    public static async Task SaveAsync_VersionIsAbsent_ExpectAddEntity()
    {
        TableEntity? actualEntity = null;
        var api = new StubTableApi
        {
            AddAsyncStub = (entity, _) =>
            {
                actualEntity = entity;

                return ValueTask.CompletedTask;
            }
        };
        var messages = new AgentChatMessage[] { new(AgentChatMessageRole.User, "Some question") };

        _ = (await new AgentConversationTableApi(api).SaveAsync(
            SomeContext,
            null,
            messages,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.NotNull(actualEntity);
        Assert.Equal("101", actualEntity.PartitionKey);
        Assert.Equal("202-303", actualEntity.RowKey);
        var messagesJson = Assert.IsType<string>(actualEntity.GetString("Messages"));
        Assert.Equal(messages, JsonSerializer.Deserialize<AgentChatMessage[]>(messagesJson));
    }

    [Fact]
    public static async Task SaveAsync_HistoryExceedsPropertyLimit_ExpectOldestMessagesDropped()
    {
        TableEntity? actualEntity = null;
        var api = new StubTableApi
        {
            UpdateAsyncStub = (entity, _, _) =>
            {
                actualEntity = entity;

                return ValueTask.CompletedTask;
            }
        };
        AgentChatMessage[] messages = Enumerable.Range(0, 20)
            .Select(index => new AgentChatMessage(
                index % 2 is 0 ? AgentChatMessageRole.User : AgentChatMessageRole.Assistant,
                $"{index}:" + new string('Ж', 2000)))
            .ToArray();

        _ = (await new AgentConversationTableApi(api).SaveAsync(
            SomeContext,
            "version-1",
            messages,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.NotNull(actualEntity);
        var messagesJson = Assert.IsType<string>(actualEntity.GetString("Messages"));
        Assert.True(Encoding.Unicode.GetByteCount(messagesJson) <= 60_000);
        var storedMessages = Assert.IsType<AgentChatMessage[]>(JsonSerializer.Deserialize<AgentChatMessage[]>(messagesJson));
        Assert.True(storedMessages.Length < messages.Length);
        Assert.True(storedMessages.Length > 1);
        Assert.Equal(AgentChatMessageRole.User, storedMessages[0].Role);
        Assert.Equal(0, storedMessages.Length % 2);
        Assert.Equal(messages[^1], storedMessages[^1]);
        Assert.Contains('Ж', messagesJson);
    }

    [Fact]
    public static async Task SaveAsync_OneMessageExceedsPropertyLimit_ExpectTextTruncated()
    {
        TableEntity? actualEntity = null;
        var api = new StubTableApi
        {
            AddAsyncStub = (entity, _) =>
            {
                actualEntity = entity;

                return ValueTask.CompletedTask;
            }
        };
        var messages = new AgentChatMessage[] { new(AgentChatMessageRole.Assistant, new string('Ж', 40_000)) };

        _ = (await new AgentConversationTableApi(api).SaveAsync(
            SomeContext,
            null,
            messages,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.NotNull(actualEntity);
        var messagesJson = Assert.IsType<string>(actualEntity.GetString("Messages"));
        Assert.True(Encoding.Unicode.GetByteCount(messagesJson) <= 60_000);
        var storedMessages = Assert.IsType<AgentChatMessage[]>(JsonSerializer.Deserialize<AgentChatMessage[]>(messagesJson));
        var storedMessage = Assert.Single(storedMessages);
        Assert.True(storedMessage.Text.Length < messages[0].Text.Length);
        Assert.EndsWith("…", storedMessage.Text);
    }

    [Theory]
    [InlineData((int)HttpStatusCode.Conflict)]
    [InlineData((int)HttpStatusCode.PreconditionFailed)]
    [InlineData((int)HttpStatusCode.NotFound)]
    public static async Task SaveAsync_RequestIsConflict_ExpectConflictFailure(int status)
    {
        var exception = new RequestFailedException(status, "Some error");
        var api = new StubTableApi
        {
            UpdateAsyncStub = (_, _, _) => ValueTask.FromException(exception)
        };

        var actual = await new AgentConversationTableApi(api).SaveAsync(
            SomeContext,
            "version-1",
            default,
            TestContext.Current.CancellationToken);

        var failure = actual.FailureOrThrow();
        Assert.Equal(AgentConversationStoreFailureCode.Conflict, failure.FailureCode);
        Assert.Same(exception, failure.SourceException);
    }
}
