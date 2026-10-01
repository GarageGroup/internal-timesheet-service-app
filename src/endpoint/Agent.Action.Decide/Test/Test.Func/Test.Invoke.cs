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
    public static async Task InvokeAsync_CreateAndDeleteConfirmDoNotFindAction_ExpectUpdateConfirmCall()
    {
        var resolver = new Mock<IAgentUserContextResolver>();
        _ = resolver.Setup(r => r.ResolveAsync(
            It.IsAny<AgentUserIdentity>(), It.IsAny<CancellationToken>())).ReturnsAsync(SomeContext);
        var createConfirmFunc = new Mock<IAgentTimesheetCreateConfirmFunc>();
        _ = createConfirmFunc.Setup(f => f.InvokeAsync(
            SomeContext, SomeActionId, It.IsAny<CancellationToken>())).ReturnsAsync(
                Failure.Create(AgentTimesheetCreateConfirmFailureCode.NotFound, "Create action not found"));
        var deleteConfirmFunc = new Mock<IAgentTimesheetDeleteConfirmFunc>();
        _ = deleteConfirmFunc.Setup(f => f.InvokeAsync(
            SomeContext, SomeActionId, It.IsAny<CancellationToken>())).ReturnsAsync(
                Failure.Create(AgentTimesheetDeleteConfirmFailureCode.NotFound, "Delete action not found"));
        var updateConfirmFunc = new Mock<IAgentTimesheetUpdateConfirmFunc>();
        _ = updateConfirmFunc.Setup(f => f.InvokeAsync(
            SomeContext, SomeActionId, It.IsAny<CancellationToken>())).ReturnsAsync(
                new AgentTimesheetUpdateConfirmOut(SomeActionId, SomeDate));
        var timesheetSetGetFunc = new Mock<IAgentTimesheetSetGetFunc>();
        var timesheetId = new Guid("7bb827d2-82ec-422e-99b0-849438089577");
        _ = timesheetSetGetFunc.Setup(f => f.InvokeAsync(
            SomeContext, new AgentTimesheetSetGetIn(SomeDate, SomeDate), It.IsAny<CancellationToken>())).ReturnsAsync(
                new AgentTimesheetSetGetOut
                {
                    Timesheets = new[] { new AgentTimesheetSetGetItem(
                        timesheetId,
                        new("e5b3afd3-ef3d-4417-9b27-6664c07cf388"),
                        ProjectType.Project,
                        "Test 01",
                        1.5m,
                        "Some description",
                        true,
                        SomeDate) }
                });
        var func = new AgentActionDecideFunc(
            resolver.Object,
            createConfirmFunc.Object,
            new Mock<IAgentTimesheetCreateCancelFunc>(MockBehavior.Strict).Object,
            deleteConfirmFunc.Object,
            new Mock<IAgentTimesheetDeleteCancelFunc>(MockBehavior.Strict).Object,
            updateConfirmFunc.Object,
            new Mock<IAgentTimesheetUpdateCancelFunc>(MockBehavior.Strict).Object,
            timesheetSetGetFunc.Object,
            new(true));

        var actual = (await func.InvokeAsync(
            SomeInput, TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(SomeActionId, actual.ActionId);
        Assert.Equal(AgentActionDecision.Confirm, actual.Decision);
        Assert.Equal(SomeDate, actual.Date);
        Assert.True(actual.TimesheetsLoaded);
        var timesheet = Assert.Single(actual.Timesheets);
        Assert.Equal(timesheetId, timesheet.Id);
        Assert.Equal("Test 01", timesheet.ProjectName);
        Assert.Equal("Project", timesheet.ProjectType);
        Assert.Equal(1.5m, timesheet.Duration);
        Assert.Equal("Some description", timesheet.Description);
        Assert.True(timesheet.IsActive);
        createConfirmFunc.VerifyAll();
        deleteConfirmFunc.VerifyAll();
        updateConfirmFunc.VerifyAll();
    }

    [Fact]
    public static async Task InvokeAsync_DecisionIsConfirm_ExpectTrustedConfirmCall()
    {
        var func = BuildFunc(
            SomeContext,
            new AgentTimesheetCreateConfirmOut(SomeActionId, SomeDate),
            new AgentTimesheetCreateCancelOut(SomeActionId),
            out var resolver,
            out var confirmFunc,
            out var cancelFunc);

        var actual = (await func.InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(new AgentActionDecideOut(SomeActionId, AgentActionDecision.Confirm) { Date = SomeDate }, actual);
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
            new AgentTimesheetCreateConfirmOut(SomeActionId, SomeDate),
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
            It.IsAny<CancellationToken>())).ReturnsAsync(new AgentTimesheetDeleteConfirmOut(SomeActionId, SomeDate));
        var deleteCancelFunc = new Mock<IAgentTimesheetDeleteCancelFunc>(MockBehavior.Strict);
        var updateConfirmFunc = new Mock<IAgentTimesheetUpdateConfirmFunc>(MockBehavior.Strict);
        var updateCancelFunc = new Mock<IAgentTimesheetUpdateCancelFunc>(MockBehavior.Strict);
        var timesheetSetGetFunc = new Mock<IAgentTimesheetSetGetFunc>();
        _ = timesheetSetGetFunc.Setup(f => f.InvokeAsync(
            SomeContext, new AgentTimesheetSetGetIn(SomeDate, SomeDate), It.IsAny<CancellationToken>())).ReturnsAsync(
                Failure.Create(AgentTimesheetSetGetFailureCode.Unknown, "Timesheets are unavailable"));
        var func = new AgentActionDecideFunc(
            resolver.Object,
            createConfirmFunc.Object,
            createCancelFunc.Object,
            deleteConfirmFunc.Object,
            deleteCancelFunc.Object,
            updateConfirmFunc.Object,
            updateCancelFunc.Object,
            timesheetSetGetFunc.Object,
            new(true));

        var actual = (await func.InvokeAsync(
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        Assert.Equal(new AgentActionDecideOut(SomeActionId, AgentActionDecision.Confirm) { Date = SomeDate }, actual);
        createConfirmFunc.VerifyAll();
        deleteConfirmFunc.VerifyAll();
        createCancelFunc.VerifyNoOtherCalls();
        deleteCancelFunc.VerifyNoOtherCalls();
        updateConfirmFunc.VerifyNoOtherCalls();
        updateCancelFunc.VerifyNoOtherCalls();
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
            new AgentTimesheetCreateConfirmOut(SomeActionId, SomeDate),
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
            new AgentTimesheetCreateConfirmOut(SomeActionId, SomeDate),
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
            new AgentTimesheetCreateConfirmOut(SomeActionId, SomeDate),
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
            new AgentTimesheetCreateConfirmOut(SomeActionId, SomeDate),
            Failure.Create(sourceCode, "Some cancel failure"),
            out _,
            out _,
            out _);

        var actual = await func.InvokeAsync(input, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, actual.FailureOrThrow().FailureCode);
    }
}
