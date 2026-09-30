extern alias ProjectGetContract;

using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;
using ProjectGetItem = ProjectGetContract::GarageGroup.Internal.Timesheet.ProjectItem;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentTimesheetCreatePrepareFuncTest
{
    [Fact]
    public static async Task InvokeAsync_InputIsValid_ExpectCanonicalPendingAction()
    {
        var canonicalProject = new ProjectGetItem(SomeInput.ProjectId, "Canonical project name", ProjectType.Incident);
        var projectOut = new ProjectSetGetOut
        {
            Projects = new[] { canonicalProject }
        };
        var func = BuildFunc(
            projectOut,
            Result.Success<Unit>(default),
            out var projectFunc,
            out var actionStore);
        AgentTimesheetCreateAction? actualAction = null;
        _ = actionStore
            .Setup(s => s.CreateAsync(
                SomeContext,
                It.IsAny<AgentTimesheetCreateAction>(),
                It.IsAny<CancellationToken>()))
            .Callback<AgentUserContext, AgentTimesheetCreateAction, CancellationToken>((_, action, _) => actualAction = action)
            .ReturnsAsync(Result.Success<Unit>(default));

        var actual = (await func.InvokeAsync(
            SomeContext,
            SomeInput,
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        var storedAction = Assert.IsType<AgentTimesheetCreateAction>(actualAction);
        Assert.NotEqual(Guid.Empty, storedAction.ActionId);
        Assert.Equal(SomeUtcNow, storedAction.CreatedAt);
        Assert.Equal(SomeUtcNow.AddMinutes(10), storedAction.ExpiresAt);
        Assert.Equal(AgentActionState.Pending, storedAction.State);
        Assert.Equal("Canonical project name", storedAction.ProjectName);
        Assert.Equal(ProjectType.Incident, storedAction.ProjectType);
        Assert.Equal(storedAction.ActionId, actual.ActionId);
        Assert.Equal(storedAction.ExpiresAt, actual.ExpiresAt);
        Assert.Equal(storedAction.ProjectName, actual.ProjectName);

        projectFunc.Verify(
            f => f.InvokeAsync(
                new ProjectSetGetIn(SomeContext.EntraObjectId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public static async Task InvokeAsync_DurationIsInvalid_ExpectInvalidDuration(decimal duration)
    {
        var func = BuildFunc(
            default(ProjectSetGetOut),
            Result.Success<Unit>(default),
            out var projectFunc,
            out var actionStore);
        var input = new AgentTimesheetCreatePrepareIn(
            SomeInput.Date,
            SomeInput.ProjectId,
            duration,
            SomeInput.Description);

        var actual = await func.InvokeAsync(SomeContext, input, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreatePrepareFailureCode.InvalidDuration, actual.FailureOrThrow().FailureCode);
        projectFunc.VerifyNoOtherCalls();
        actionStore.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ProjectIsNotReturned_ExpectInvalidProject()
    {
        var func = BuildFunc(
            new ProjectSetGetOut { Projects = default },
            Result.Success<Unit>(default),
            out _,
            out var actionStore);

        var actual = await func.InvokeAsync(SomeContext, SomeInput, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreatePrepareFailureCode.InvalidProject, actual.FailureOrThrow().FailureCode);
        actionStore.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_StoreReturnsConflict_ExpectConflict()
    {
        var projectOut = new ProjectSetGetOut
        {
            Projects = new[] { new ProjectGetItem(SomeInput.ProjectId, "Some project", ProjectType.Project) }
        };
        var func = BuildFunc(
            projectOut,
            Failure.Create(AgentActionStoreFailureCode.Conflict, "Some conflict"),
            out _,
            out _);

        var actual = await func.InvokeAsync(SomeContext, SomeInput, TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetCreatePrepareFailureCode.Conflict, actual.FailureOrThrow().FailureCode);
    }
}
