using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Storage.Table.Test;

partial class AgentActionTableApiTest
{
    [Fact]
    public static async Task GetAsync_EntityIsFoundForOwner_ExpectMappedAction()
    {
        var entity = BuildEntity();
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (partitionKey, rowKey, _) =>
            {
                Assert.Equal("101", partitionKey);
                Assert.Equal("84e6c5b815974a2e821af392c8c0ae3d", rowKey);

                return ValueTask.FromResult<TableEntity?>(entity);
            }
        };

        var actual = (await new AgentActionTableApi(tableApi).GetAsync(
            SomeContext,
            SomeAction.ActionId,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(SomeAction with { Version = "version-1" }, actual);
    }

    [Fact]
    public static async Task GetAsync_EntityBelongsToAnotherUser_ExpectNotFound()
    {
        var entity = BuildEntity();
        entity["TelegramUserId"] = 404L;
        var tableApi = new StubTableApi
        {
            GetAsyncStub = (_, _, _) => ValueTask.FromResult<TableEntity?>(entity)
        };

        var actual = (await new AgentActionTableApi(tableApi).GetAsync(
            SomeContext,
            SomeAction.ActionId,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Null(actual);
    }

    private static TableEntity BuildEntity()
        =>
        new("101", "84e6c5b815974a2e821af392c8c0ae3d")
        {
            ETag = new ETag("version-1"),
            ["TelegramUserId"] = SomeContext.TelegramUserId,
            ["TelegramChatId"] = SomeContext.TelegramChatId,
            ["BindingId"] = SomeContext.BindingId,
            ["CrmSystemUserId"] = SomeContext.CrmSystemUserId,
            ["EntraObjectId"] = SomeContext.EntraObjectId,
            ["Date"] = "2026-09-29",
            ["ProjectId"] = SomeAction.ProjectId,
            ["ProjectName"] = SomeAction.ProjectName,
            ["ProjectType"] = (int)SomeAction.ProjectType,
            ["Duration"] = "1.5",
            ["Description"] = SomeAction.Description,
            ["CreatedAt"] = SomeAction.CreatedAt,
            ["ExpiresAt"] = SomeAction.ExpiresAt,
            ["State"] = (int)AgentActionState.Pending
        };
}
