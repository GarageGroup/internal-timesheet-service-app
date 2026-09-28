using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

partial class AgentReadPlugin
{
    [KernelFunction("get_recent_projects")]
    [Description("Returns projects recently used by the current user for time entries.")]
    public async Task<AgentReadToolResult<AgentLastProjectSetGetOut>> GetRecentProjectsAsync(
        [Description("Optional maximum number of projects to return.")] int? top,
        CancellationToken cancellationToken)
    {
        var result = await lastProjectSetGetFunc.InvokeAsync(
            context,
            new(top),
            cancellationToken);

        return result.IsSuccess
            ? BuildSuccess(result.SuccessOrThrow())
            : BuildFailure<AgentLastProjectSetGetOut, AgentLastProjectSetGetFailureCode>(result.FailureOrThrow().FailureCode);
    }
}
