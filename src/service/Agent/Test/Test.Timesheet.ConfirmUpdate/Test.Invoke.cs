using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentTimesheetUpdateConfirmFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ActionIsPendingAndUpdateSucceeds_ExpectTrustedCallerAndSucceededTransition()
    {
        var action = new AgentTimesheetUpdateAction(
            SomeActionId,
            new("46606dc6-335f-4271-86b7-ff9540e9f480"),
            new(2026, 09, 30),
            new("8a511343-6d42-f111-88b3-000d3a24cd41"),
            "Test 01",
            ProjectType.Project,
            1.5m,
            "Updated",
            SomeUtcNow.AddMinutes(-1),
            SomeUtcNow.AddMinutes(9),
            version: "version-1");
        var actionStore = new Mock<IAgentTimesheetUpdateActionStore>();
        _ = actionStore.SetupSequence(s => s.GetUpdateAsync(SomeContext, SomeActionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(action)
            .ReturnsAsync(action with { State = AgentActionState.Executing, Version = "version-2" });
        _ = actionStore.Setup(s => s.UpdateStateAsync(
            SomeContext, SomeActionId, It.IsAny<string>(), It.IsAny<AgentActionState>(),
            It.IsAny<AgentActionState>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<Unit>(default));
        var updateFunc = new Mock<ITimesheetUpdateFunc>();
        _ = updateFunc.Setup(f => f.UpdateAsync(
            new TimesheetUpdateIn(
                SomeContext.EntraObjectId,
                action.TimesheetId,
                action.Date,
                new TimesheetProject(action.ProjectId, action.ProjectType),
                action.Duration,
                action.Description),
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<Unit>(default));
        var func = new AgentTimesheetUpdateConfirmFunc(actionStore.Object, updateFunc.Object, new TestDateProvider());

        var actual = (await func.InvokeAsync(
            SomeContext, SomeActionId, TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(SomeActionId, actual.ActionId);
        actionStore.Verify(s => s.UpdateStateAsync(
            SomeContext, SomeActionId, "version-1", AgentActionState.Pending,
            AgentActionState.Executing, It.IsAny<CancellationToken>()), Times.Once);
        actionStore.Verify(s => s.UpdateStateAsync(
            SomeContext, SomeActionId, "version-2", AgentActionState.Executing,
            AgentActionState.Succeeded, CancellationToken.None), Times.Once);
        updateFunc.VerifyAll();
    }

    [Fact]
    public static async Task InvokeAsync_ActionIsNotFound_ExpectNotFoundWithoutUpdate()
    {
        var actionStore = new Mock<IAgentTimesheetUpdateActionStore>();
        _ = actionStore.Setup(s => s.GetUpdateAsync(
            SomeContext, SomeActionId, It.IsAny<CancellationToken>())).ReturnsAsync(default(AgentTimesheetUpdateAction));
        var updateFunc = new Mock<ITimesheetUpdateFunc>(MockBehavior.Strict);
        var func = new AgentTimesheetUpdateConfirmFunc(actionStore.Object, updateFunc.Object, new TestDateProvider());

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetUpdateConfirmFailureCode.NotFound, actual.FailureOrThrow().FailureCode);
        updateFunc.VerifyNoOtherCalls();
    }
}
