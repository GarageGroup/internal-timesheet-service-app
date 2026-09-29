using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentTimesheetDeleteConfirmFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ActionIsPendingAndDeleteSucceeds_ExpectTrustedCallerAndSucceededTransition()
    {
        var action = BuildAction();
        var executingAction = action with { State = AgentActionState.Executing, Version = "version-2" };
        var actionStore = new Mock<IAgentTimesheetDeleteActionStore>();
        _ = actionStore
            .SetupSequence(s => s.GetDeleteAsync(
                SomeContext,
                SomeActionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(action)
            .ReturnsAsync(executingAction);
        _ = actionStore
            .Setup(s => s.UpdateStateAsync(
                SomeContext,
                SomeActionId,
                It.IsAny<string>(),
                It.IsAny<AgentActionState>(),
                It.IsAny<AgentActionState>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Unit>(default));
        var deleteFunc = new Mock<ITimesheetDeleteFunc>();
        _ = deleteFunc
            .Setup(f => f.InvokeAsync(
                new TimesheetDeleteIn(SomeContext.EntraObjectId, action.TimesheetId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Unit>(default));
        var func = new AgentTimesheetDeleteConfirmFunc(actionStore.Object, deleteFunc.Object, new TestDateProvider());

        var actual = (await func.InvokeAsync(
            SomeContext,
            SomeActionId,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(SomeActionId, actual.ActionId);
        actionStore.Verify(s => s.UpdateStateAsync(
            SomeContext,
            SomeActionId,
            "version-1",
            AgentActionState.Pending,
            AgentActionState.Executing,
            It.IsAny<CancellationToken>()), Times.Once);
        actionStore.Verify(s => s.UpdateStateAsync(
            SomeContext,
            SomeActionId,
            "version-2",
            AgentActionState.Executing,
            AgentActionState.Succeeded,
            CancellationToken.None), Times.Once);
        deleteFunc.VerifyAll();
    }

    [Fact]
    public static async Task InvokeAsync_ActionIsNotFound_ExpectNotFoundWithoutDelete()
    {
        var actionStore = new Mock<IAgentTimesheetDeleteActionStore>();
        _ = actionStore.Setup(s => s.GetDeleteAsync(
            SomeContext,
            SomeActionId,
            It.IsAny<CancellationToken>())).ReturnsAsync(default(AgentTimesheetDeleteAction));
        var deleteFunc = new Mock<ITimesheetDeleteFunc>(MockBehavior.Strict);
        var func = new AgentTimesheetDeleteConfirmFunc(actionStore.Object, deleteFunc.Object, new TestDateProvider());

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetDeleteConfirmFailureCode.NotFound, actual.FailureOrThrow().FailureCode);
        deleteFunc.VerifyNoOtherCalls();
    }
}
