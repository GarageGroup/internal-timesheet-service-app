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
}
