using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

partial class AgentReadPlugin
{
    [KernelFunction("get_project_tags")]
    [Description("Returns tag suggestions for a project already selected from the user's project results.")]
    public async Task<AgentReadToolResult<AgentTagSetGetOut>> GetProjectTagsAsync(
        [Description("Project identifier returned by search_projects or get_recent_projects.")] Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await tagSetGetFunc.InvokeAsync(
            context,
            new(projectId),
            cancellationToken);

        return result.IsSuccess
            ? BuildSuccess(result.SuccessOrThrow())
            : BuildFailure<AgentTagSetGetOut, AgentTagSetGetFailureCode>(result.FailureOrThrow().FailureCode);
    }
}
