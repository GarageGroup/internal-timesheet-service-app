using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentTimesheetSetGetFuncTest
{
    [Fact]
    public static async Task InvokeAsync_DateFromIsLaterThanDateTo_ExpectInvalidDateRangeFailure()
    {
        var mockTimesheetFunc = BuildMockTimesheetFunc(default(TimesheetSetGetOut));
        var func = new AgentTimesheetSetGetFunc(mockTimesheetFunc.Object, SomeOption);

        var actual = await func.InvokeAsync(
            SomeContext,
            new(new(2026, 9, 29), new(2026, 9, 28)),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetSetGetFailureCode.InvalidDateRange, actual.FailureOrThrow().FailureCode);
        mockTimesheetFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_DateRangeIsTooLong_ExpectDateRangeTooLongFailure()
    {
        var mockTimesheetFunc = BuildMockTimesheetFunc(default(TimesheetSetGetOut));
        var func = new AgentTimesheetSetGetFunc(mockTimesheetFunc.Object, SomeOption);

        var actual = await func.InvokeAsync(
            SomeContext,
            new(new(2026, 8, 28), new(2026, 9, 28)),
            TestContext.Current.CancellationToken);

        Assert.Equal(AgentTimesheetSetGetFailureCode.DateRangeTooLong, actual.FailureOrThrow().FailureCode);
        mockTimesheetFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task InvokeAsync_ValidInput_ExpectTrustedEntraObjectIdPassedToTimesheetFunc()
    {
        var mockTimesheetFunc = BuildMockTimesheetFunc(default(TimesheetSetGetOut));
        var func = new AgentTimesheetSetGetFunc(mockTimesheetFunc.Object, SomeOption);
        var input = new AgentTimesheetSetGetIn(new(2026, 9, 1), new(2026, 9, 28));

        _ = await func.InvokeAsync(SomeContext, input, TestContext.Current.CancellationToken);

        var expected = new TimesheetSetGetIn(SomeContext.EntraObjectId, input.DateFrom, input.DateTo);
        mockTimesheetFunc.Verify(f => f.InvokeAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public static async Task InvokeAsync_TimesheetFuncResultIsFailure_ExpectUnknownFailure()
    {
        var sourceException = new Exception("Some error");
        var mockTimesheetFunc = BuildMockTimesheetFunc(sourceException.ToFailure("Failed to get timesheets"));
        var func = new AgentTimesheetSetGetFunc(mockTimesheetFunc.Object, SomeOption);

        var actual = await func.InvokeAsync(
            SomeContext,
            new(new(2026, 9, 1), new(2026, 9, 28)),
            TestContext.Current.CancellationToken);

        var expected = Failure.Create(
            AgentTimesheetSetGetFailureCode.Unknown,
            "Failed to get timesheets",
            sourceException);

        Assert.StrictEqual(expected, actual.FailureOrThrow());
    }

    [Fact]
    public static async Task InvokeAsync_TimesheetFuncResultIsSuccess_ExpectMappedTimesheets()
    {
        var timesheet = new TimesheetSetGetItem(
            id: new("62e36d0c-a697-4687-813d-b7e528746df0"),
            projectId: new("e5d78b74-752a-42ea-8e6c-5a4a0267620b"),
            projectType: ProjectType.Project,
            projectName: "Some project",
            duration: 2.5m,
            description: "Some description",
            isActive: true,
            date: new(2026, 9, 28))
        {
            ProjectComment = "Some comment"
        };

        var mockTimesheetFunc = BuildMockTimesheetFunc(
            new TimesheetSetGetOut
            {
                Timesheets = [timesheet]
            });

        var func = new AgentTimesheetSetGetFunc(mockTimesheetFunc.Object, SomeOption);
        var actual = await func.InvokeAsync(
            SomeContext,
            new(new(2026, 9, 28), new(2026, 9, 28)),
            TestContext.Current.CancellationToken);

        var expected = new AgentTimesheetSetGetOut
        {
            Timesheets =
            [
                new(
                    id: timesheet.Id,
                    projectId: timesheet.ProjectId,
                    projectType: timesheet.ProjectType,
                    projectName: timesheet.ProjectName,
                    duration: timesheet.Duration,
                    description: timesheet.Description,
                    isActive: timesheet.IsActive,
                    date: timesheet.Date)
                {
                    ProjectComment = timesheet.ProjectComment
                }
            ]
        };

        Assert.StrictEqual(expected, actual.SuccessOrThrow());
    }
}
