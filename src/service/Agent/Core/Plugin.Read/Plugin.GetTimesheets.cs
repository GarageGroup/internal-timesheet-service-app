using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

partial class AgentReadPlugin
{
    [KernelFunction("get_timesheets")]
    [Description("Returns the current user's time entries for the specified inclusive date range.")]
    public async Task<AgentReadToolResult<AgentTimesheetSetGetOut>> GetTimesheetsAsync(
        [Description("First date of the range in YYYY-MM-DD format.")] DateOnly dateFrom,
        [Description("Last date of the range in YYYY-MM-DD format.")] DateOnly dateTo,
        CancellationToken cancellationToken)
    {
        var result = await timesheetSetGetFunc.InvokeAsync(
            context,
            new(dateFrom, dateTo),
            cancellationToken);

        return result.IsSuccess
            ? BuildSuccess(result.SuccessOrThrow())
            : BuildFailure<AgentTimesheetSetGetOut, AgentTimesheetSetGetFailureCode>(result.FailureOrThrow().FailureCode);
    }
}
