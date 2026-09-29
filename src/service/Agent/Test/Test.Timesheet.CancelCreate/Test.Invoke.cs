using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentTimesheetCreateCancelFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ActionIsPending_ExpectCancelledTransition()
    {
        var action = BuildAction();
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(action),
            Result.Success<Unit>(default),
            out var actionStore);

        var actual = (await func.InvokeAsync(
            SomeContext,
            SomeActionId,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(SomeActionId, actual.ActionId);
        actionStore.Verify(
            s => s.UpdateStateAsync(
                SomeContext,
                SomeActionId,
                "version-1",
                AgentActionState.Pending,
                AgentActionState.Cancelled,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_ActionIdIsEmpty_ExpectInvalidActionId()
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(BuildAction()),
            Result.Success<Unit>(default),
            out var actionStore);

        var actual = await func.InvokeAsync(SomeContext, Guid.Empty, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateCancelFailureCode.InvalidActionId, actual.FailureOrThrow().FailureCode);
        actionStore.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ActionIsNotFound_ExpectNotFound()
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(null),
            Result.Success<Unit>(default),
            out _);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateCancelFailureCode.NotFound, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task InvokeAsync_ActionIsNotPending_ExpectInvalidState()
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(BuildAction(AgentActionState.Executing)),
            Result.Success<Unit>(default),
            out _);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateCancelFailureCode.InvalidState, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task InvokeAsync_ActionTimeIsExpired_ExpectExpiredTransition()
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(BuildAction(expiresAt: SomeUtcNow)),
            Result.Success<Unit>(default),
            out var actionStore);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateCancelFailureCode.Expired, actual.FailureOrThrow().FailureCode);
        actionStore.Verify(
            s => s.UpdateStateAsync(
                SomeContext,
                SomeActionId,
                "version-1",
                AgentActionState.Pending,
                AgentActionState.Expired,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_ConcurrentUpdateReturnsConflict_ExpectConflict()
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(BuildAction()),
            Failure.Create(AgentActionStoreFailureCode.Conflict, "Some conflict"),
            out _);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateCancelFailureCode.Conflict, actual.FailureOrThrow().FailureCode);
    }
}
