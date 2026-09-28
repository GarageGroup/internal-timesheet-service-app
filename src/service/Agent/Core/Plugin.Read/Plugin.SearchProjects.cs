using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

partial class AgentReadPlugin
{
    [KernelFunction("search_projects")]
    [Description("Searches projects available to the current user by name or code.")]
    public async Task<AgentReadToolResult<AgentProjectSetSearchOut>> SearchProjectsAsync(
        [Description("Text from the project name or code to search for.")] string searchText,
        [Description("Optional maximum number of projects to return.")] int? top,
        CancellationToken cancellationToken)
    {
        var result = await projectSetSearchFunc.InvokeAsync(
            context,
            new(searchText, top),
            cancellationToken);

        return result.IsSuccess
            ? BuildSuccess(result.SuccessOrThrow())
            : BuildFailure<AgentProjectSetSearchOut, AgentProjectSetSearchFailureCode>(result.FailureOrThrow().FailureCode);
    }
}
