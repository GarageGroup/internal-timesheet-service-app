using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

partial class AgentReadPlugin
{
    [KernelFunction("get_periods")]
    [Description("Returns the date periods available for time entry operations.")]
    public async Task<AgentReadToolResult<AgentPeriodSetGetOut>> GetPeriodsAsync(CancellationToken cancellationToken)
    {
        var result = await periodSetGetFunc.InvokeAsync(context, cancellationToken);

        return result.IsSuccess
            ? BuildSuccess(result.SuccessOrThrow())
            : BuildFailure<AgentPeriodSetGetOut, AgentPeriodSetGetFailureCode>(result.FailureOrThrow().FailureCode);
    }
}
