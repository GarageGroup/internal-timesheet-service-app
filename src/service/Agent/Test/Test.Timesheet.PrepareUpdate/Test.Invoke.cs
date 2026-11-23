extern alias ProjectGetContract;

using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using ProjectGetItem = ProjectGetContract::GarageGroup.Internal.Timesheet.ProjectItem;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentTimesheetUpdatePrepareFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ProjectIsChanged_ExpectCanonicalPendingAction()
    {
        var project = new ProjectGetItem(
            new("7712b133-f72f-4b52-a0cc-78d0882ba84f"),
            "Canonical incident",
            ProjectType.Incident);
        var func = BuildFunc(
            SomeTimesheet,
            project,
            Result.Success<Unit>(default),
            out var timesheetFunc,
            out var projectFunc,
            out var actionStore);
        AgentTimesheetUpdateAction? storedAction = null;
        _ = actionStore
            .Setup(s => s.CreateAsync(
                SomeContext,
                It.IsAny<AgentTimesheetUpdateAction>(),
                It.IsAny<CancellationToken>()))
            .Callback<AgentUserContext, AgentTimesheetUpdateAction, CancellationToken>((_, action, _) => storedAction = action)
            .ReturnsAsync(Result.Success<Unit>(default));
        var input = new AgentTimesheetUpdatePrepareIn(
            SomeTimesheet.Id,
            SomeTimesheet.Date,
            new(2026, 09, 29),
            project.Id,
            2.5m,
            "Updated description");

        var actual = (await func.InvokeAsync(
            SomeContext,
            input,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        var action = Assert.IsType<AgentTimesheetUpdateAction>(storedAction);
        Assert.Equal(SomeTimesheet.Id, action.TimesheetId);
        Assert.Equal(new DateOnly(2026, 09, 29), action.Date);
        Assert.Equal(project.Id, action.ProjectId);
        Assert.Equal(project.Name, action.ProjectName);
        Assert.Equal(ProjectType.Incident, action.ProjectType);
        Assert.Equal(2.5m, action.Duration);
        Assert.Equal("Updated description", action.Description);
        Assert.Equal(SomeUtcNow, action.CreatedAt);
        Assert.Equal(SomeUtcNow.AddMinutes(10), action.ExpiresAt);
        Assert.Equal(action.ActionId, actual.ActionId);

        timesheetFunc.Verify(
            f => f.InvokeAsync(
                SomeContext,
                new AgentTimesheetSetGetIn(SomeTimesheet.Date, SomeTimesheet.Date),
                It.IsAny<CancellationToken>()),
            Times.Once);
        projectFunc.Verify(
            f => f.InvokeAsync(
                new ProjectSetGetIn(SomeContext.EntraObjectId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_DurationOnlyIsChanged_ExpectOriginalCanonicalValues()
    {
        var func = BuildFunc(
            SomeTimesheet,
            null,
            Result.Success<Unit>(default),
            out _,
            out var projectFunc,
            out var actionStore);
        AgentTimesheetUpdateAction? storedAction = null;
        _ = actionStore
            .Setup(s => s.CreateAsync(
                SomeContext,
                It.IsAny<AgentTimesheetUpdateAction>(),
                It.IsAny<CancellationToken>()))
            .Callback<AgentUserContext, AgentTimesheetUpdateAction, CancellationToken>((_, action, _) => storedAction = action)
            .ReturnsAsync(Result.Success<Unit>(default));

        _ = (await func.InvokeAsync(
            SomeContext,
            new(SomeTimesheet.Id, SomeTimesheet.Date, null, null, 3m, null),
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        var action = Assert.IsType<AgentTimesheetUpdateAction>(storedAction);
        Assert.Equal(SomeTimesheet.Date, action.Date);
        Assert.Equal(SomeTimesheet.ProjectId, action.ProjectId);
        Assert.Equal(SomeTimesheet.ProjectName, action.ProjectName);
        Assert.Equal(SomeTimesheet.ProjectType, action.ProjectType);
        Assert.Equal(3m, action.Duration);
        Assert.Equal(SomeTimesheet.Description, action.Description);
        projectFunc.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public static async Task InvokeAsync_DurationIsInvalid_ExpectInvalidDuration(decimal duration)
    {
        var func = BuildFunc(
            SomeTimesheet,
            null,
            Result.Success<Unit>(default),
            out var timesheetFunc,
            out _,
            out var actionStore);

        var actual = await func.InvokeAsync(
            SomeContext,
            new(SomeTimesheet.Id, SomeTimesheet.Date, null, null, duration, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetUpdatePrepareFailureCode.InvalidDuration, actual.FailureOrThrow().FailureCode);
        timesheetFunc.VerifyNoOtherCalls();
        actionStore.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_TimesheetIsNotReturned_ExpectNotFound()
    {
        var func = BuildFunc(
            null,
            null,
            Result.Success<Unit>(default),
            out _,
            out _,
            out var actionStore);

        var actual = await func.InvokeAsync(
            SomeContext,
            new(SomeTimesheet.Id, SomeTimesheet.Date, null, null, 2m, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetUpdatePrepareFailureCode.NotFound, actual.FailureOrThrow().FailureCode);
        actionStore.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_TimesheetIsReadOnly_ExpectReadOnly()
    {
        var readOnlyTimesheet = new AgentTimesheetSetGetItem(
            SomeTimesheet.Id,
            SomeTimesheet.ProjectId,
            SomeTimesheet.ProjectType,
            SomeTimesheet.ProjectName,
            SomeTimesheet.Duration,
            SomeTimesheet.Description,
            false,
            SomeTimesheet.Date);
        var func = BuildFunc(
            readOnlyTimesheet,
            null,
            Result.Success<Unit>(default),
            out _,
            out _,
            out var actionStore);

        var actual = await func.InvokeAsync(
            SomeContext,
            new(SomeTimesheet.Id, SomeTimesheet.Date, null, null, 2m, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetUpdatePrepareFailureCode.ReadOnly, actual.FailureOrThrow().FailureCode);
        actionStore.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ValuesAreUnchanged_ExpectEmptyChanges()
    {
        var func = BuildFunc(
            SomeTimesheet,
            null,
            Result.Success<Unit>(default),
            out _,
            out _,
            out var actionStore);

        var actual = await func.InvokeAsync(
            SomeContext,
            new(SomeTimesheet.Id, SomeTimesheet.Date, null, null, SomeTimesheet.Duration, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetUpdatePrepareFailureCode.EmptyChanges, actual.FailureOrThrow().FailureCode);
        actionStore.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ProjectIsNotReturned_ExpectInvalidProject()
    {
        var func = BuildFunc(
            SomeTimesheet,
            null,
            Result.Success<Unit>(default),
            out _,
            out _,
            out var actionStore);

        var actual = await func.InvokeAsync(
            SomeContext,
            new(SomeTimesheet.Id, SomeTimesheet.Date, null, Guid.NewGuid(), null, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetUpdatePrepareFailureCode.InvalidProject, actual.FailureOrThrow().FailureCode);
        actionStore.VerifyNoOtherCalls();
    }
}
