using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Endpoint.Agent.Action.Decide.Test;

partial class AgentActionDecideFuncTest
{
    [Fact]
    public static async Task InvokeAsync_DecisionIsConfirm_ExpectTrustedConfirmCall()
    {
        var func = BuildFunc(
            SomeContext,
            new AgentTimesheetCreateConfirmOut(SomeActionId),
            new AgentTimesheetCreateCancelOut(SomeActionId),
            out var resolver,
            out var confirmFunc,
            out var cancelFunc);

        var actual = (await func.InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(new AgentActionDecideOut(SomeActionId, AgentActionDecision.Confirm), actual);
        resolver.Verify(
            r => r.ResolveAsync(
                new AgentUserIdentity(101, 202, 202),
                It.IsAny<CancellationToken>()),
            Times.Once);
        confirmFunc.Verify(
            f => f.InvokeAsync(
                SomeContext,
                SomeActionId,
                It.IsAny<CancellationToken>()),
            Times.Once);
        cancelFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_DecisionIsCancel_ExpectTrustedCancelCall()
    {
        var input = new AgentActionDecideIn(101, SomeActionId, 303, 202, 202, AgentActionDecision.Cancel);
        var func = BuildFunc(
            SomeContext,
            new AgentTimesheetCreateConfirmOut(SomeActionId),
            new AgentTimesheetCreateCancelOut(SomeActionId),
            out _,
            out var confirmFunc,
            out var cancelFunc);

        var actual = (await func.InvokeAsync(
            input,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(new AgentActionDecideOut(SomeActionId, AgentActionDecision.Cancel), actual);
        cancelFunc.Verify(
            f => f.InvokeAsync(
                SomeContext,
                SomeActionId,
                It.IsAny<CancellationToken>()),
            Times.Once);
        confirmFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_CreateConfirmDoesNotFindAction_ExpectDeleteConfirmCall()
    {
        var resolver = new Mock<IAgentUserContextResolver>();
        _ = resolver.Setup(r => r.ResolveAsync(
            It.IsAny<AgentUserIdentity>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(SomeContext);
        var createConfirmFunc = new Mock<IAgentTimesheetCreateConfirmFunc>();
        _ = createConfirmFunc.Setup(f => f.InvokeAsync(
            SomeContext,
            SomeActionId,
            It.IsAny<CancellationToken>())).ReturnsAsync(
                Failure.Create(AgentTimesheetCreateConfirmFailureCode.NotFound, "Create action not found"));
        var createCancelFunc = new Mock<IAgentTimesheetCreateCancelFunc>(MockBehavior.Strict);
        var deleteConfirmFunc = new Mock<IAgentTimesheetDeleteConfirmFunc>();
        _ = deleteConfirmFunc.Setup(f => f.InvokeAsync(
            SomeContext,
            SomeActionId,
            It.IsAny<CancellationToken>())).ReturnsAsync(new AgentTimesheetDeleteConfirmOut(SomeActionId));
        var deleteCancelFunc = new Mock<IAgentTimesheetDeleteCancelFunc>(MockBehavior.Strict);
        var func = new AgentActionDecideFunc(
            resolver.Object,
            createConfirmFunc.Object,
            createCancelFunc.Object,
            deleteConfirmFunc.Object,
            deleteCancelFunc.Object,
            new(true));

        var actual = (await func.InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(new AgentActionDecideOut(SomeActionId, AgentActionDecision.Confirm), actual);
        createConfirmFunc.VerifyAll();
        deleteConfirmFunc.VerifyAll();
        createCancelFunc.VerifyNoOtherCalls();
        deleteCancelFunc.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 303, AgentActionDecision.Confirm)]
    [InlineData(1, 0, AgentActionDecision.Confirm)]
    [InlineData(1, 303, (AgentActionDecision)10)]
    public static async Task InvokeAsync_InputIsInvalid_ExpectInvalidDecision(
        int actionIdCase,
        long telegramUpdateId,
        AgentActionDecision decision)
    {
        var actionId = actionIdCase == 0 ? Guid.Empty : SomeActionId;
        var input = new AgentActionDecideIn(101, actionId, telegramUpdateId, 202, 202, decision);
        var func = BuildFunc(
            SomeContext,
            new AgentTimesheetCreateConfirmOut(SomeActionId),
            new AgentTimesheetCreateCancelOut(SomeActionId),
            out var resolver,
            out var confirmFunc,
            out var cancelFunc);

        var actual = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        Assert.Equal(AgentActionDecideFailureCode.InvalidDecision, actual.FailureOrThrow().FailureCode);
        resolver.VerifyNoOtherCalls();
        confirmFunc.VerifyNoOtherCalls();
        cancelFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_WriteIsDisabled_ExpectWriteDisabled()
    {
        var func = BuildFunc(
            SomeContext,
            new AgentTimesheetCreateConfirmOut(SomeActionId),
            new AgentTimesheetCreateCancelOut(SomeActionId),
            out var resolver,
            out var confirmFunc,
            out var cancelFunc,
            enabled: false);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        Assert.Equal(AgentActionDecideFailureCode.WriteDisabled, actual.FailureOrThrow().FailureCode);
        resolver.VerifyNoOtherCalls();
        confirmFunc.VerifyNoOtherCalls();
        cancelFunc.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(AgentUserContextResolveFailureCode.InvalidIdentity, AgentActionDecideFailureCode.InvalidIdentity)]
    [InlineData(AgentUserContextResolveFailureCode.UnsupportedChat, AgentActionDecideFailureCode.InvalidIdentity)]
    [InlineData(AgentUserContextResolveFailureCode.UserNotLinked, AgentActionDecideFailureCode.UserNotLinked)]
    [InlineData(AgentUserContextResolveFailureCode.AmbiguousBinding, AgentActionDecideFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.BindingSignedOut, AgentActionDecideFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.UserDisabled, AgentActionDecideFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.MissingEntraObjectId, AgentActionDecideFailureCode.UserUnavailable)]
    [InlineData(AgentUserContextResolveFailureCode.Unknown, AgentActionDecideFailureCode.Unknown)]
    public static async Task InvokeAsync_UserResolveFails_ExpectMappedFailure(
        AgentUserContextResolveFailureCode sourceCode,
        AgentActionDecideFailureCode expectedCode)
    {
        var func = BuildFunc(
            Failure.Create(sourceCode, "Some user failure"),
            new AgentTimesheetCreateConfirmOut(SomeActionId),
            new AgentTimesheetCreateCancelOut(SomeActionId),
            out _,
            out var confirmFunc,
            out var cancelFunc);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, actual.FailureOrThrow().FailureCode);
        confirmFunc.VerifyNoOtherCalls();
        cancelFunc.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(AgentTimesheetCreateConfirmFailureCode.InvalidActionId, AgentActionDecideFailureCode.InvalidDecision)]
    [InlineData(AgentTimesheetCreateConfirmFailureCode.NotFound, AgentActionDecideFailureCode.ActionNotFound)]
    [InlineData(AgentTimesheetCreateConfirmFailureCode.Expired, AgentActionDecideFailureCode.ActionExpired)]
    [InlineData(AgentTimesheetCreateConfirmFailureCode.InvalidState, AgentActionDecideFailureCode.InvalidActionState)]
    [InlineData(AgentTimesheetCreateConfirmFailureCode.Conflict, AgentActionDecideFailureCode.ActionConflict)]
    [InlineData(AgentTimesheetCreateConfirmFailureCode.BadRequest, AgentActionDecideFailureCode.InvalidTimesheet)]
    [InlineData(AgentTimesheetCreateConfirmFailureCode.Forbidden, AgentActionDecideFailureCode.TimesheetForbidden)]
    [InlineData(AgentTimesheetCreateConfirmFailureCode.ProjectNotFound, AgentActionDecideFailureCode.ProjectNotFound)]
    [InlineData(AgentTimesheetCreateConfirmFailureCode.Indeterminate, AgentActionDecideFailureCode.Indeterminate)]
    [InlineData(AgentTimesheetCreateConfirmFailureCode.Unknown, AgentActionDecideFailureCode.Unknown)]
    public static async Task InvokeAsync_ConfirmFails_ExpectMappedFailure(
        AgentTimesheetCreateConfirmFailureCode sourceCode,
        AgentActionDecideFailureCode expectedCode)
    {
        var func = BuildFunc(
            SomeContext,
            Failure.Create(sourceCode, "Some confirm failure"),
            new AgentTimesheetCreateCancelOut(SomeActionId),
            out _,
            out _,
            out _);

        var actual = await func.InvokeAsync(SomeInput, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, actual.FailureOrThrow().FailureCode);
    }

    [Theory]
    [InlineData(AgentTimesheetCreateCancelFailureCode.InvalidActionId, AgentActionDecideFailureCode.InvalidDecision)]
    [InlineData(AgentTimesheetCreateCancelFailureCode.NotFound, AgentActionDecideFailureCode.ActionNotFound)]
    [InlineData(AgentTimesheetCreateCancelFailureCode.Expired, AgentActionDecideFailureCode.ActionExpired)]
    [InlineData(AgentTimesheetCreateCancelFailureCode.InvalidState, AgentActionDecideFailureCode.InvalidActionState)]
    [InlineData(AgentTimesheetCreateCancelFailureCode.Conflict, AgentActionDecideFailureCode.ActionConflict)]
    [InlineData(AgentTimesheetCreateCancelFailureCode.Unknown, AgentActionDecideFailureCode.Unknown)]
    public static async Task InvokeAsync_CancelFails_ExpectMappedFailure(
        AgentTimesheetCreateCancelFailureCode sourceCode,
        AgentActionDecideFailureCode expectedCode)
    {
        var input = new AgentActionDecideIn(101, SomeActionId, 303, 202, 202, AgentActionDecision.Cancel);
        var func = BuildFunc(
            SomeContext,
            new AgentTimesheetCreateConfirmOut(SomeActionId),
            Failure.Create(sourceCode, "Some cancel failure"),
            out _,
            out _,
            out _);

        var actual = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, actual.FailureOrThrow().FailureCode);
    }
}
