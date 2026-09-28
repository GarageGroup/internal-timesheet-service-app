using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test;

partial class AgentRequestTableApiTest
{
    [Fact]
    public static async Task CreateAsync_RequestIsNew_ExpectQueuedRequest()
    {
        TableEntity? actualEntity = null;
        var tableApi = new StubTableApi
        {
            AddAsyncStub = (entity, _) =>
            {
                actualEntity = entity;

                return ValueTask.CompletedTask;
            }
        };

        var actual = (await new AgentRequestTableApi(tableApi).CreateAsync(
            SomeContext,
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.NotNull(actualEntity);
        Assert.Equal("101", actualEntity.PartitionKey);
        Assert.Equal(64, actualEntity.RowKey.Length);
        Assert.Equal(actualEntity.RowKey, actual.RequestId);
        Assert.Equal(303, actual.TelegramUpdateId);
        Assert.Equal("Some question", actual.Text);
        Assert.Equal("ru", actual.Locale);
        Assert.Equal(AgentRequestStatus.Queued, actual.Status);
    }

    [Fact]
    public static async Task CreateAsync_SameUpdateIsRepeated_ExpectExistingRequest()
    {
        var sourceEntity = CreateSourceEntity();
        var tableApi = new StubTableApi
        {
            AddAsyncStub = static (_, _) => ValueTask.FromException(
                new RequestFailedException((int)HttpStatusCode.Conflict, "Already exists")),
            GetAsyncStub = (_, rowKey, _) =>
            {
                Assert.Equal(sourceEntity.RowKey, rowKey);

                return ValueTask.FromResult<TableEntity?>(sourceEntity);
            }
        };

        var actual = (await new AgentRequestTableApi(tableApi).CreateAsync(
            SomeContext,
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(sourceEntity.RowKey, actual.RequestId);
        Assert.Equal("version-1", actual.Version);
    }

    [Fact]
    public static async Task CreateAsync_UpdateHasDifferentPayload_ExpectConflict()
    {
        var sourceEntity = CreateSourceEntity();
        sourceEntity["Text"] = "Another question";
        var tableApi = new StubTableApi
        {
            AddAsyncStub = static (_, _) => ValueTask.FromException(
                new RequestFailedException((int)HttpStatusCode.Conflict, "Already exists")),
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(sourceEntity)
        };

        var actual = await new AgentRequestTableApi(tableApi).CreateAsync(
            SomeContext,
            SomeInput,
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentRequestStoreFailureCode.Conflict, actual.FailureOrThrow().FailureCode);
    }

    private static TableEntity CreateSourceEntity()
        =>
        new("101", "116f7071f75495ccb9c1e58a0a57ceb81b58995ee58708d5ad655314247899b6")
        {
            ETag = new("version-1"),
            ["TelegramUserId"] = 202L,
            ["TelegramChatId"] = 202L,
            ["TelegramUpdateId"] = 303L,
            ["Text"] = "Some question",
            ["Locale"] = "ru",
            ["Status"] = "Queued"
        };
}
