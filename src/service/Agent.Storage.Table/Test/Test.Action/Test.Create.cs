using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test;

partial class AgentActionTableApiTest
{
    [Fact]
    public static async Task CreateAsync_InputIsValid_ExpectOwnedPendingEntity()
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

        _ = (await new AgentActionTableApi(tableApi).CreateAsync(
            SomeContext,
            SomeAction,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.NotNull(actualEntity);
        Assert.Equal("101", actualEntity.PartitionKey);
        Assert.Equal("84e6c5b815974a2e821af392c8c0ae3d", actualEntity.RowKey);
        Assert.Equal(202, actualEntity.GetInt64("TelegramUserId"));
        Assert.Equal(303, actualEntity.GetInt64("TelegramChatId"));
        Assert.Equal(SomeContext.BindingId, actualEntity.GetGuid("BindingId"));
        Assert.Equal(SomeContext.CrmSystemUserId, actualEntity.GetGuid("CrmSystemUserId"));
        Assert.Equal(SomeContext.EntraObjectId, actualEntity.GetGuid("EntraObjectId"));
        Assert.Equal("2026-09-29", actualEntity.GetString("Date"));
        Assert.Equal("1.5", actualEntity.GetString("Duration"));
        Assert.Equal((int)AgentActionState.Pending, actualEntity.GetInt32("State"));
    }

    [Fact]
    public static async Task CreateAsync_ActionAlreadyExists_ExpectConflictFailure()
    {
        var exception = new RequestFailedException((int)HttpStatusCode.Conflict, "Some error");
        var tableApi = new StubTableApi
        {
            AddAsyncStub = (_, _) => ValueTask.FromException(exception)
        };

        var actual = await new AgentActionTableApi(tableApi).CreateAsync(
            SomeContext,
            SomeAction,
            TestContext.Current.CancellationToken);

        var failure = actual.FailureOrThrow();
        Assert.Equal(AgentActionStoreFailureCode.Conflict, failure.FailureCode);
        Assert.Same(exception, failure.SourceException);
    }
}
