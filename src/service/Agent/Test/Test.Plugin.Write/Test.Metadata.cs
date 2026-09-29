using System.Linq;
using Microsoft.SemanticKernel;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentWritePluginTest
{
    [Fact]
    public static void CreateFromObject_ExpectOnlyPrepareFunction()
    {
        var plugin = KernelPluginFactory.CreateFromObject(
            CreatePlugin(BuildPrepareFunc(SomeOutput)),
            AgentWritePlugin.PluginName);

        var function = Assert.Single(plugin);
        Assert.Equal(AgentWritePlugin.PluginName, plugin.Name);
        Assert.Equal("prepare_create_timesheet", function.Name);
        Assert.DoesNotContain(plugin, static item => item.Name.Contains("confirm", System.StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(plugin, static item => item.Name.Contains("execute", System.StringComparison.OrdinalIgnoreCase));
    }
}
