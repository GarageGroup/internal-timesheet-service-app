using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentKernelFactoryTest
{
    [Fact]
    public static void Create_ExpectFoundryChatServiceAndReadPlugin()
    {
        var kernel = CreateFactory().Create(SomeContext);

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var functions = kernel.Plugins[AgentReadPlugin.PluginName].Select(static function => function.Name).Order().ToArray();

        Assert.NotNull(chatService);
        Assert.Equal(
            ["get_periods", "get_project_tags", "get_recent_projects", "get_timesheets", "search_projects"],
            functions);
    }

    [Fact]
    public static async Task Create_InvokePlugin_ExpectFactoryContext()
    {
        var mockPeriodFunc = new Mock<IAgentPeriodSetGetFunc>();
        var source = new AgentPeriodSetGetOut { Periods = default };

        _ = mockPeriodFunc.Setup(
            f => f.InvokeAsync(SomeContext, It.IsAny<CancellationToken>()))
        .ReturnsAsync(source);

        var kernel = CreateFactory(mockPeriodFunc).Create(SomeContext);
        var actual = await kernel.InvokeAsync<AgentReadToolResult<AgentPeriodSetGetOut>>(
            AgentReadPlugin.PluginName,
            "get_periods",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(actual.IsSuccess);
        Assert.Equal(source, actual.Data);
        mockPeriodFunc.VerifyAll();
    }

    [Fact]
    public static async Task Create_InvokeTimesheetsPluginWithStringDates_ExpectTypedInput()
    {
        var mockTimesheetFunc = new Mock<IAgentTimesheetSetGetFunc>();
        var dateFrom = new DateOnly(2026, 9, 28);
        var source = new AgentTimesheetSetGetOut { Timesheets = default };

        _ = mockTimesheetFunc.Setup(
            f => f.InvokeAsync(SomeContext, new(dateFrom, dateFrom), It.IsAny<CancellationToken>()))
        .ReturnsAsync(source);

        var kernel = CreateFactory(timesheetFunc: mockTimesheetFunc).Create(SomeContext);
        var arguments = new KernelArguments
        {
            ["dateFrom"] = "2026-09-28",
            ["dateTo"] = "2026-09-28"
        };

        var actual = await kernel.InvokeAsync<AgentReadToolResult<AgentTimesheetSetGetOut>>(
            AgentReadPlugin.PluginName,
            "get_timesheets",
            arguments,
            TestContext.Current.CancellationToken);

        Assert.True(actual.IsSuccess);
        Assert.Equal(source, actual.Data);
        mockTimesheetFunc.VerifyAll();
    }
}
