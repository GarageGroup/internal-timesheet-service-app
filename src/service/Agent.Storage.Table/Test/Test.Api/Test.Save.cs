using System;
using System.Net;
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
