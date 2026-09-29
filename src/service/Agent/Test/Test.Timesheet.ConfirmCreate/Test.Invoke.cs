using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentTimesheetCreateConfirmFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ActionIsPending_ExpectExecutingTransition()
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
                AgentActionState.Executing,
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

        Assert.Equal(AgentTimesheetCreateConfirmFailureCode.InvalidActionId, actual.FailureOrThrow().FailureCode);
        actionStore.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ActionIsNotFound_ExpectNotFound()
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(null),
            Result.Success<Unit>(default),
            out var actionStore);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateConfirmFailureCode.NotFound, actual.FailureOrThrow().FailureCode);
        actionStore.Verify(
            s => s.GetAsync(SomeContext, SomeActionId, It.IsAny<CancellationToken>()),
            Times.Once);
        actionStore.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(AgentActionState.Executing)]
    [InlineData(AgentActionState.Succeeded)]
    [InlineData(AgentActionState.Failed)]
    [InlineData(AgentActionState.Cancelled)]
    [InlineData(AgentActionState.Indeterminate)]
    public static async Task InvokeAsync_ActionIsNotPending_ExpectInvalidState(AgentActionState state)
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(BuildAction(state)),
            Result.Success<Unit>(default),
            out var actionStore);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateConfirmFailureCode.InvalidState, actual.FailureOrThrow().FailureCode);
        actionStore.Verify(
            s => s.GetAsync(SomeContext, SomeActionId, It.IsAny<CancellationToken>()),
            Times.Once);
        actionStore.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ActionStateIsExpired_ExpectExpired()
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(BuildAction(AgentActionState.Expired)),
            Result.Success<Unit>(default),
            out _);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateConfirmFailureCode.Expired, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task InvokeAsync_ActionTimeIsExpired_ExpectExpiredTransition()
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(BuildAction(expiresAt: SomeUtcNow)),
            Result.Success<Unit>(default),
            out var actionStore);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateConfirmFailureCode.Expired, actual.FailureOrThrow().FailureCode);
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
    public static async Task InvokeAsync_VersionIsMissing_ExpectConflict()
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(BuildAction(version: null)),
            Result.Success<Unit>(default),
            out _);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateConfirmFailureCode.Conflict, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task InvokeAsync_ConcurrentUpdateReturnsConflict_ExpectConflict()
    {
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(BuildAction()),
            Failure.Create(AgentActionStoreFailureCode.Conflict, "Some conflict"),
            out _);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateConfirmFailureCode.Conflict, actual.FailureOrThrow().FailureCode);
    }
}
