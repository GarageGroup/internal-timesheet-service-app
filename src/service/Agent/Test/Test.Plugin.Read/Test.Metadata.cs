using System.Linq;
using Microsoft.SemanticKernel;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentReadPluginTest
{
    [Fact]
    public static void CreateFromObject_ExpectOnlyAllowedReadFunctions()
    {
        var plugin = KernelPluginFactory.CreateFromObject(CreatePlugin(), AgentReadPlugin.PluginName);

        var actual = plugin.Select(static function => function.Name).Order().ToArray();
        string[] expected =
        [
            "get_periods",
            "get_project_tags",
            "get_recent_projects",
            "get_timesheets",
            "search_projects"
        ];

        Assert.Equal(AgentReadPlugin.PluginName, plugin.Name);
        Assert.Equal(expected, actual);
    }
}
