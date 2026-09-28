using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test;

partial class AgentRequestTableApiTest
{
    [Fact]
    public static async Task UpdateAsync_RequestIsActual_ExpectUpdatedEntity()
    {
        var sourceEntity = CreateSourceEntity();
        TableEntity? actualEntity = null;
        ETag? actualEtag = null;
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(sourceEntity),
            UpdateAsyncStub = (entity, etag, _) =>
            {
                actualEntity = entity;
                actualEtag = etag;

                return ValueTask.CompletedTask;
            }
        };
        var input = new AgentRequestUpdateIn(
            sourceEntity.RowKey,
            "version-1",
            AgentRequestStatus.Queued,
            AgentRequestStatus.Completed,
            "Some response",
            null);

        _ = (await new AgentRequestTableApi(tableApi).UpdateAsync(
            SomeContext,
            input,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(new ETag("version-1"), actualEtag);
        Assert.NotNull(actualEntity);
        Assert.Equal("Completed", actualEntity.GetString("Status"));
        Assert.Equal("Some response", actualEntity.GetString("ResponseText"));
        Assert.False(actualEntity.ContainsKey("FailureCode"));
        Assert.Equal(202, actualEntity.GetInt64("TelegramUserId"));
        Assert.Equal(202, actualEntity.GetInt64("TelegramChatId"));
    }

    [Fact]
    public static async Task UpdateAsync_StatusWasChanged_ExpectConflictWithoutUpdate()
    {
        var sourceEntity = CreateSourceEntity();
        sourceEntity["Status"] = "Running";
        var updateCalled = false;
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(sourceEntity),
            UpdateAsyncStub = (_, _, _) =>
            {
                updateCalled = true;

                return ValueTask.CompletedTask;
            }
        };
        var input = new AgentRequestUpdateIn(
            sourceEntity.RowKey,
            "version-1",
            AgentRequestStatus.Queued,
            AgentRequestStatus.Running,
            null,
            null);

        var actual = await new AgentRequestTableApi(tableApi).UpdateAsync(
            SomeContext,
            input,
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentRequestStoreFailureCode.Conflict, actual.FailureOrThrow().FailureCode);
        Assert.False(updateCalled);
    }

    [Theory]
    [InlineData((int)HttpStatusCode.Conflict, AgentRequestStoreFailureCode.Conflict)]
    [InlineData((int)HttpStatusCode.PreconditionFailed, AgentRequestStoreFailureCode.Conflict)]
    [InlineData((int)HttpStatusCode.NotFound, AgentRequestStoreFailureCode.NotFound)]
    public static async Task UpdateAsync_TableUpdateHasFailed_ExpectMappedFailure(
        int status,
        AgentRequestStoreFailureCode expectedFailureCode)
    {
        var sourceException = new RequestFailedException(status, "Some error");
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(CreateSourceEntity()),
            UpdateAsyncStub = (_, _, _) => ValueTask.FromException(sourceException)
        };
        var input = new AgentRequestUpdateIn(
            CreateSourceEntity().RowKey,
            "version-1",
            AgentRequestStatus.Queued,
            AgentRequestStatus.Running,
            null,
            null);

        var actual = await new AgentRequestTableApi(tableApi).UpdateAsync(
            SomeContext,
            input,
            TestContext.Current.CancellationToken);

        var failure = actual.FailureOrThrow();
        Assert.Equal(expectedFailureCode, failure.FailureCode);
        Assert.Same(sourceException, failure.SourceException);
    }
}
