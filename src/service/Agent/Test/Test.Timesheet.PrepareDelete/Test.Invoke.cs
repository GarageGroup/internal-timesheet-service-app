using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentTimesheetDeletePrepareFuncTest
{
    [Fact]
    public static async Task InvokeAsync_TimesheetIsAvailable_ExpectCanonicalAction()
    {
        var item = new AgentTimesheetSetGetItem(
            SomeTimesheetId, Guid.NewGuid(), ProjectType.Project, "Project", 1.5m, "Description", true, SomeDate);
        var func = BuildFunc(
            new AgentTimesheetSetGetOut { Timesheets = new[] { item } },
            out var timesheetFunc,
            out var actionStore);
        AgentTimesheetDeleteAction? storedAction = null;
        _ = actionStore.Setup(s => s.CreateAsync(
            SomeContext,
            It.IsAny<AgentTimesheetDeleteAction>(),
            It.IsAny<CancellationToken>()))
            .Callback<AgentUserContext, AgentTimesheetDeleteAction, CancellationToken>((_, action, _) => storedAction = action)
            .ReturnsAsync(Result.Success<Unit>(default));

        var actual = (await func.InvokeAsync(
            SomeContext,
            new(SomeTimesheetId, SomeDate),
            TestContext.Current.CancellationToken)).SuccessOrThrow();

        var action = Assert.IsType<AgentTimesheetDeleteAction>(storedAction);
        Assert.Equal(item.Id, action.TimesheetId);
        Assert.Equal(item.ProjectName, action.ProjectName);
        Assert.Equal(SomeUtcNow.AddMinutes(10), action.ExpiresAt);
        Assert.Equal(action.ActionId, actual.ActionId);
        timesheetFunc.Verify(f => f.InvokeAsync(
            SomeContext,
            new AgentTimesheetSetGetIn(SomeDate, SomeDate),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_IdIsNotReturned_ExpectNotFound()
    {
        var func = BuildFunc(new AgentTimesheetSetGetOut { Timesheets = default }, out _, out var actionStore);

        var actual = await func.InvokeAsync(
            SomeContext,
            new(SomeTimesheetId, SomeDate),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetDeletePrepareFailureCode.NotFound, actual.FailureOrThrow().FailureCode);
        actionStore.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_TimesheetIsReadOnly_ExpectReadOnly()
    {
        var item = new AgentTimesheetSetGetItem(
            SomeTimesheetId, Guid.NewGuid(), ProjectType.Project, "Project", 1m, "Description", false, SomeDate);
        var func = BuildFunc(new AgentTimesheetSetGetOut { Timesheets = new[] { item } }, out _, out var actionStore);

        var actual = await func.InvokeAsync(
            SomeContext,
            new(SomeTimesheetId, SomeDate),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetDeletePrepareFailureCode.ReadOnly, actual.FailureOrThrow().FailureCode);
        actionStore.VerifyNoOtherCalls();
    }
}
