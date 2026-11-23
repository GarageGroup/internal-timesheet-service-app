using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentReadPluginTest
{
    [Fact]
    public static async Task GetTimesheetsAsync_ExpectTrustedContextAndInput()
    {
        var mockFunc = new Mock<IAgentTimesheetSetGetFunc>();
        var dateFrom = new DateOnly(2026, 9, 1);
        var dateTo = new DateOnly(2026, 9, 28);
        var source = new AgentTimesheetSetGetOut { Timesheets = default };

        _ = mockFunc.Setup(
            f => f.InvokeAsync(SomeContext, new(dateFrom, dateTo), It.IsAny<CancellationToken>()))
        .ReturnsAsync(source);

        var actual = await CreatePlugin(timesheetFunc: mockFunc).GetTimesheetsAsync(
            "2026-09-01",
            "2026-09-28",
            TestContext.Current.CancellationToken);

        Assert.True(actual.IsSuccess);
        Assert.Equal(source, actual.Data);
        Assert.Null(actual.ErrorCode);
        mockFunc.VerifyAll();
    }

    [Theory]
    [InlineData("01.09.2026", "2026-09-28")]
    [InlineData("2026-09-01", "28.09.2026")]
    [InlineData("", "2026-09-28")]
    public static async Task GetTimesheetsAsync_InvalidDate_ExpectSafeErrorCode(string dateFrom, string dateTo)
    {
        var mockFunc = new Mock<IAgentTimesheetSetGetFunc>(MockBehavior.Strict);

        var actual = await CreatePlugin(timesheetFunc: mockFunc).GetTimesheetsAsync(
            dateFrom,
            dateTo,
            TestContext.Current.CancellationToken);

        Assert.False(actual.IsSuccess);
        Assert.Null(actual.Data);
        Assert.Equal(nameof(AgentTimesheetSetGetFailureCode.InvalidDateFormat), actual.ErrorCode);
        mockFunc.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task SearchProjectsAsync_ExpectTrustedContextAndInput()
    {
        var mockFunc = new Mock<IAgentProjectSetSearchFunc>();
        var source = new AgentProjectSetSearchOut { Projects = default };

        _ = mockFunc.Setup(
            f => f.InvokeAsync(
                SomeContext,
                It.Is<AgentProjectSetSearchIn>(static input => input.SearchText == "Project" && input.Top == 7),
                It.IsAny<CancellationToken>()))
        .ReturnsAsync(source);

        var actual = await CreatePlugin(projectSearchFunc: mockFunc).SearchProjectsAsync(
            "Project",
            7,
            TestContext.Current.CancellationToken);

        Assert.True(actual.IsSuccess);
        Assert.Equal(source, actual.Data);
        mockFunc.VerifyAll();
    }

    [Fact]
    public static async Task GetRecentProjectsAsync_ExpectTrustedContextAndInput()
    {
        var mockFunc = new Mock<IAgentLastProjectSetGetFunc>();
        var source = new AgentLastProjectSetGetOut { Projects = default };

        _ = mockFunc.Setup(
            f => f.InvokeAsync(SomeContext, new(8), It.IsAny<CancellationToken>()))
        .ReturnsAsync(source);

        var actual = await CreatePlugin(lastProjectFunc: mockFunc).GetRecentProjectsAsync(
            8,
            TestContext.Current.CancellationToken);

        Assert.True(actual.IsSuccess);
        Assert.Equal(source, actual.Data);
        mockFunc.VerifyAll();
    }

    [Fact]
    public static async Task GetPeriodsAsync_ExpectTrustedContext()
    {
        var mockFunc = new Mock<IAgentPeriodSetGetFunc>();
        var source = new AgentPeriodSetGetOut { Periods = default };

        _ = mockFunc.Setup(
            f => f.InvokeAsync(SomeContext, It.IsAny<CancellationToken>()))
        .ReturnsAsync(source);

        var actual = await CreatePlugin(periodFunc: mockFunc).GetPeriodsAsync(TestContext.Current.CancellationToken);

        Assert.True(actual.IsSuccess);
        Assert.Equal(source, actual.Data);
        mockFunc.VerifyAll();
    }

    [Fact]
    public static async Task GetProjectTagsAsync_ExpectTrustedContextAndInput()
    {
        var mockFunc = new Mock<IAgentTagSetGetFunc>();
        var projectId = Guid.Parse("023909fd-5d76-4ff6-a2d1-18e8559cbcd7");
        var source = new AgentTagSetGetOut { Tags = default };

        _ = mockFunc.Setup(
            f => f.InvokeAsync(SomeContext, new(projectId), It.IsAny<CancellationToken>()))
        .ReturnsAsync(source);

        var actual = await CreatePlugin(tagFunc: mockFunc).GetProjectTagsAsync(
            projectId,
            TestContext.Current.CancellationToken);

        Assert.True(actual.IsSuccess);
        Assert.Equal(source, actual.Data);
        mockFunc.VerifyAll();
    }

    [Fact]
    public static async Task GetProjectTagsAsync_Failure_ExpectSafeErrorCode()
    {
        var mockFunc = new Mock<IAgentTagSetGetFunc>();
        var failure = Failure.Create(AgentTagSetGetFailureCode.InvalidProjectId, "Sensitive failure message");

        _ = mockFunc.Setup(
            f => f.InvokeAsync(SomeContext, It.IsAny<AgentTagSetGetIn>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(failure);

        var actual = await CreatePlugin(tagFunc: mockFunc).GetProjectTagsAsync(
            Guid.Empty,
            TestContext.Current.CancellationToken);

        Assert.False(actual.IsSuccess);
        Assert.Null(actual.Data);
        Assert.Equal(nameof(AgentTagSetGetFailureCode.InvalidProjectId), actual.ErrorCode);
    }
}
