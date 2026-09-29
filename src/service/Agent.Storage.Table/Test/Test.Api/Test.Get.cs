using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test;

partial class AgentConversationTableApiTest
{
    [Fact]
    public static async Task GetAsync_EntityIsNotFound_ExpectEmptyConversation()
    {
        var store = new AgentConversationTableApi(new StubTableApi());

        var actual = (await store.GetAsync(SomeContext, TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Empty(actual.Messages.AsEnumerable());
        Assert.Null(actual.Version);
    }

    [Fact]
    public static async Task GetAsync_EntityIsFound_ExpectMappedConversation()
    {
        var entity = new TableEntity("101", "202-303")
        {
            ETag = new ETag("version-1"),
            ["Messages"] = "[{\"Role\":0,\"Text\":\"Some question\"}]"
        };
        var api = new StubTableApi
        {
            GetAsyncStub = (partitionKey, rowKey, _) =>
            {
                Assert.Equal("101", partitionKey);
                Assert.Equal("202-303", rowKey);

                return ValueTask.FromResult<TableEntity?>(entity);
            }
        };

        var actual = (await new AgentConversationTableApi(api).GetAsync(
            SomeContext,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal("version-1", actual.Version);
        Assert.Equal(new(AgentChatMessageRole.User, "Some question"), Assert.Single(actual.Messages.AsEnumerable()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("null")]
    public static async Task GetAsync_MessagesAreMissingOrNull_ExpectUnknownFailure(string? messages)
    {
        var entity = new TableEntity("101", "202-303")
        {
            ETag = new ETag("version-1")
        };
        if (messages is not null)
        {
            entity["Messages"] = messages;
        }

        var api = new StubTableApi
        {
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(entity)
        };

        var actual = await new AgentConversationTableApi(api).GetAsync(
            SomeContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentConversationStoreFailureCode.Unknown, actual.FailureOrThrow().FailureCode);
    }
}
