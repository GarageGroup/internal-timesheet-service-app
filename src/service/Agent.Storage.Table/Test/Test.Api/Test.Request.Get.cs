using System.Threading.Tasks;
using Azure.Data.Tables;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test;

partial class AgentRequestTableApiTest
{
    [Fact]
    public static async Task GetAsync_RequestBelongsToContext_ExpectRequest()
    {
        var sourceEntity = CreateSourceEntity();
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (partitionKey, rowKey, _) =>
            {
                Assert.Equal("101", partitionKey);
                Assert.Equal(sourceEntity.RowKey, rowKey);

                return ValueTask.FromResult<TableEntity?>(sourceEntity);
            }
        };

        var actual = (await new AgentRequestTableApi(tableApi).GetAsync(
            SomeContext,
            sourceEntity.RowKey,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(sourceEntity.RowKey, actual.RequestId);
        Assert.Equal(AgentRequestStatus.Queued, actual.Status);
    }

    [Fact]
    public static async Task GetAsync_RequestBelongsToAnotherUser_ExpectNotFound()
    {
        var sourceEntity = CreateSourceEntity();
        sourceEntity["TelegramUserId"] = 404L;
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(sourceEntity)
        };

        var actual = await new AgentRequestTableApi(tableApi).GetAsync(
            SomeContext,
            sourceEntity.RowKey,
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentRequestStoreFailureCode.NotFound, actual.FailureOrThrow().FailureCode);
    }
}
