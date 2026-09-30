using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

public static class AgentTimesheetUpdateCancelFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ActionIsPending_ExpectCancelledTransition()
    {
        var utcNow = new DateTimeOffset(2026, 09, 30, 12, 00, 00, TimeSpan.Zero);
        var context = new AgentUserContext(
            101, 202, 303,
            new("80ae312e-305b-49dd-a905-d39e30d11385"),
            new("ff66af05-eccc-4c7d-b6a7-98a56e39c6e9"),
            new("bcf9aa86-35b6-4e97-9bc2-3477d94a519e"));
        var action = new AgentTimesheetUpdateAction(
            new("78302d93-e6dc-4fd6-be63-2480c8984382"),
            new("46606dc6-335f-4271-86b7-ff9540e9f480"),
            new(2026, 09, 30),
            new("8a511343-6d42-f111-88b3-000d3a24cd41"),
            "Test 01",
            ProjectType.Project,
            1.5m,
            "Updated",
            utcNow.AddMinutes(-1),
            utcNow.AddMinutes(9),
            version: "version-1");
        var actionStore = new Mock<IAgentTimesheetUpdateActionStore>();
        _ = actionStore.Setup(s => s.GetUpdateAsync(
            context, action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(action);
        _ = actionStore.Setup(s => s.UpdateStateAsync(
            context, action.ActionId, "version-1", AgentActionState.Pending,
            AgentActionState.Cancelled, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<Unit>(default));
        var func = new AgentTimesheetUpdateCancelFunc(actionStore.Object, new TestDateProvider(utcNow));

        var actual = (await func.InvokeAsync(
            context, action.ActionId, TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(action.ActionId, actual.ActionId);
        actionStore.VerifyAll();
    }

    private sealed class TestDateProvider(DateTimeOffset utcNow) : IDateProvider
    {
        public DateTimeOffset UtcNow => utcNow;

        public DateOnly Today => DateOnly.FromDateTime(utcNow.UtcDateTime);
    }
}
