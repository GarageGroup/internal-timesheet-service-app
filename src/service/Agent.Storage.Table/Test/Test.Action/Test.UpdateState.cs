using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test;

partial class AgentActionTableApiTest
{
    [Fact]
    public static async Task UpdateStateAsync_CurrentStateMatches_ExpectConditionalUpdate()
    {
        var entity = BuildEntity();
        TableEntity? actualEntity = null;
        ETag? actualEtag = null;
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(entity),
            UpdateAsyncStub = (@in, etag, _) =>
            {
                actualEntity = @in;
                actualEtag = etag;

                return ValueTask.CompletedTask;
            }
        };

        _ = (await new AgentActionTableApi(tableApi).UpdateStateAsync(
            SomeContext,
            SomeAction.ActionId,
            "version-1",
            AgentActionState.Pending,
            AgentActionState.Executing,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        var updatedEntity = Assert.IsType<TableEntity>(actualEntity);
        Assert.Same(entity, updatedEntity);
        Assert.Equal(new ETag("version-1"), actualEtag);
        Assert.Equal((int)AgentActionState.Executing, updatedEntity.GetInt32("State"));
    }

    [Fact]
    public static async Task UpdateStateAsync_EntityBelongsToAnotherBinding_ExpectConflict()
    {
        var entity = BuildEntity();
        entity["BindingId"] = new System.Guid("719c5084-d45d-4b24-82e6-bfa171405af9");
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(entity),
            UpdateAsyncStub = (_, _, _) => ValueTask.FromException(new Xunit.Sdk.XunitException("Update must not be called"))
        };

        var actual = await new AgentActionTableApi(tableApi).UpdateStateAsync(
            SomeContext,
            SomeAction.ActionId,
            "version-1",
            AgentActionState.Pending,
            AgentActionState.Executing,
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentActionStoreFailureCode.Conflict, actual.FailureOrThrow().FailureCode);
    }

    [Theory]
    [InlineData("another-version", AgentActionState.Pending)]
    [InlineData("version-1", AgentActionState.Executing)]
    public static async Task UpdateStateAsync_ExpectedValueDoesNotMatch_ExpectConflict(
        string expectedVersion, AgentActionState expectedState)
    {
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(BuildEntity()),
            UpdateAsyncStub = (_, _, _) => ValueTask.FromException(new Xunit.Sdk.XunitException("Update must not be called"))
        };

        var actual = await new AgentActionTableApi(tableApi).UpdateStateAsync(
            SomeContext,
            SomeAction.ActionId,
            expectedVersion,
            expectedState,
            AgentActionState.Executing,
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentActionStoreFailureCode.Conflict, actual.FailureOrThrow().FailureCode);
    }

    [Theory]
    [InlineData((int)HttpStatusCode.Conflict)]
    [InlineData((int)HttpStatusCode.PreconditionFailed)]
    [InlineData((int)HttpStatusCode.NotFound)]
    public static async Task UpdateStateAsync_ConditionalUpdateFails_ExpectConflict(int status)
    {
        var exception = new RequestFailedException(status, "Some error");
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(BuildEntity()),
            UpdateAsyncStub = (_, _, _) => ValueTask.FromException(exception)
        };

        var actual = await new AgentActionTableApi(tableApi).UpdateStateAsync(
            SomeContext,
            SomeAction.ActionId,
            "version-1",
            AgentActionState.Pending,
            AgentActionState.Executing,
            TestContext.Current.CancellationToken);

        var failure = actual.FailureOrThrow();
        Assert.Equal(AgentActionStoreFailureCode.Conflict, failure.FailureCode);
        Assert.Same(exception, failure.SourceException);
    }
}
