using System.Linq;
using Microsoft.SemanticKernel;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.Agent.Test;

partial class AgentWritePluginTest
{
    [Fact]
    public static void CreateFromObject_ExpectOnlyPrepareFunctions()
    {
        var plugin = KernelPluginFactory.CreateFromObject(
            CreatePlugin(BuildPrepareFunc(SomeOutput)),
            AgentWritePlugin.PluginName);

        Assert.Equal(AgentWritePlugin.PluginName, plugin.Name);
        Assert.Equal(
            ["prepare_create_timesheet", "prepare_delete_timesheet", "prepare_update_timesheet"],
            plugin.Select(static function => function.Name).Order().ToArray());
        Assert.DoesNotContain(plugin, static item => item.Name.Contains("confirm", System.StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(plugin, static item => item.Name.Contains("execute", System.StringComparison.OrdinalIgnoreCase));
    }
}
