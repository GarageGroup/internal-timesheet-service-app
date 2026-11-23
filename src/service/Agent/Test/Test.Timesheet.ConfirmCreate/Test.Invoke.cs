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
    public static async Task InvokeAsync_ActionIsPendingAndCreateSucceeds_ExpectSucceededTransition()
    {
        var action = BuildAction();
        var executingAction = action with
        {
            State = AgentActionState.Executing,
            Version = "version-2"
        };
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(action),
            Result.Success<Unit>(default),
            Result.Success<Unit>(default),
            out var actionStore,
            out var timesheetCreateFunc);
        _ = actionStore
            .SetupSequence(s => s.GetAsync(
                SomeContext,
                SomeActionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(action)
            .ReturnsAsync(executingAction);

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
        actionStore.Verify(
            s => s.UpdateStateAsync(
                SomeContext,
                SomeActionId,
                "version-2",
                AgentActionState.Executing,
                AgentActionState.Succeeded,
                CancellationToken.None),
            Times.Once);
        timesheetCreateFunc.Verify(
            f => f.CreateAsync(
                new TimesheetCreateIn(
                    SomeContext.EntraObjectId,
                    action.Date,
                    new(action.ProjectId, action.ProjectType),
                    action.Duration,
                    action.Description),
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

    [Theory]
    [InlineData(TimesheetCreateFailureCode.BadRequest, AgentTimesheetCreateConfirmFailureCode.BadRequest)]
    [InlineData(TimesheetCreateFailureCode.UnexpectedProjectType, AgentTimesheetCreateConfirmFailureCode.BadRequest)]
    [InlineData(TimesheetCreateFailureCode.EmptyDescription, AgentTimesheetCreateConfirmFailureCode.BadRequest)]
    [InlineData(TimesheetCreateFailureCode.Forbidden, AgentTimesheetCreateConfirmFailureCode.Forbidden)]
    [InlineData(TimesheetCreateFailureCode.ProjectNotFound, AgentTimesheetCreateConfirmFailureCode.ProjectNotFound)]
    public static async Task InvokeAsync_CreateReturnsBusinessFailure_ExpectFailedTransition(
        TimesheetCreateFailureCode createFailureCode,
        AgentTimesheetCreateConfirmFailureCode expectedFailureCode)
    {
        var action = BuildAction();
        var executingAction = action with
        {
            State = AgentActionState.Executing,
            Version = "version-2"
        };
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(action),
            Result.Success<Unit>(default),
            Failure.Create(createFailureCode, "Some create failure"),
            out var actionStore,
            out _);
        _ = actionStore
            .SetupSequence(s => s.GetAsync(
                SomeContext,
                SomeActionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(action)
            .ReturnsAsync(executingAction);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(expectedFailureCode, actual.FailureOrThrow().FailureCode);
        actionStore.Verify(
            s => s.UpdateStateAsync(
                SomeContext,
                SomeActionId,
                "version-2",
                AgentActionState.Executing,
                AgentActionState.Failed,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_CreateReturnsUnknownFailure_ExpectIndeterminateTransition()
    {
        var action = BuildAction();
        var executingAction = action with
        {
            State = AgentActionState.Executing,
            Version = "version-2"
        };
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(action),
            Result.Success<Unit>(default),
            Failure.Create(TimesheetCreateFailureCode.Unknown, "Some unknown failure"),
            out var actionStore,
            out _);
        _ = actionStore
            .SetupSequence(s => s.GetAsync(
                SomeContext,
                SomeActionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(action)
            .ReturnsAsync(executingAction);

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateConfirmFailureCode.Indeterminate, actual.FailureOrThrow().FailureCode);
        actionStore.Verify(
            s => s.UpdateStateAsync(
                SomeContext,
                SomeActionId,
                "version-2",
                AgentActionState.Executing,
                AgentActionState.Indeterminate,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_ResultStateCannotBePersisted_ExpectIndeterminate()
    {
        var action = BuildAction();
        var executingAction = action with
        {
            State = AgentActionState.Executing,
            Version = "version-2"
        };
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(action),
            Result.Success<Unit>(default),
            Result.Success<Unit>(default),
            out var actionStore,
            out _);
        _ = actionStore
            .SetupSequence(s => s.GetAsync(
                SomeContext,
                SomeActionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(action)
            .ReturnsAsync(executingAction);
        _ = actionStore
            .SetupSequence(s => s.UpdateStateAsync(
                SomeContext,
                SomeActionId,
                It.IsAny<string>(),
                It.IsAny<AgentActionState>(),
                It.IsAny<AgentActionState>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Unit>(default))
            .ReturnsAsync(Failure.Create(AgentActionStoreFailureCode.Conflict, "Some conflict"));

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateConfirmFailureCode.Indeterminate, actual.FailureOrThrow().FailureCode);
    }

    [Fact]
    public static async Task InvokeAsync_CreateThrowsException_ExpectIndeterminateTransition()
    {
        var action = BuildAction();
        var executingAction = action with
        {
            State = AgentActionState.Executing,
            Version = "version-2"
        };
        var func = BuildFunc(
            Result.Success<AgentTimesheetCreateAction?>(action),
            Result.Success<Unit>(default),
            Result.Success<Unit>(default),
            out var actionStore,
            out var timesheetCreateFunc);
        _ = actionStore
            .SetupSequence(s => s.GetAsync(
                SomeContext,
                SomeActionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(action)
            .ReturnsAsync(executingAction);
        _ = timesheetCreateFunc
            .Setup(f => f.CreateAsync(
                It.IsAny<TimesheetCreateIn>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Some create exception"));

        var actual = await func.InvokeAsync(SomeContext, SomeActionId, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreateConfirmFailureCode.Indeterminate, actual.FailureOrThrow().FailureCode);
        actionStore.Verify(
            s => s.UpdateStateAsync(
                SomeContext,
                SomeActionId,
                "version-2",
                AgentActionState.Executing,
                AgentActionState.Indeterminate,
                CancellationToken.None),
            Times.Once);
    }
}
