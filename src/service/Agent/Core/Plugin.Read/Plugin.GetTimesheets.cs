using System;
using System.ComponentModel;
using System.Globalization;
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
        [Description("First date of the range in YYYY-MM-DD format.")] string dateFrom,
        [Description("Last date of the range in YYYY-MM-DD format.")] string dateTo,
        CancellationToken cancellationToken)
    {
        if (TryParseDate(dateFrom, out var parsedDateFrom) is false ||
            TryParseDate(dateTo, out var parsedDateTo) is false)
        {
            return BuildFailure<AgentTimesheetSetGetOut, AgentTimesheetSetGetFailureCode>(
                AgentTimesheetSetGetFailureCode.InvalidDateFormat);
        }

        var result = await timesheetSetGetFunc.InvokeAsync(
            context,
            new(parsedDateFrom, parsedDateTo),
            cancellationToken);

        return result.IsSuccess
            ? BuildSuccess(result.SuccessOrThrow())
            : BuildFailure<AgentTimesheetSetGetOut, AgentTimesheetSetGetFailureCode>(result.FailureOrThrow().FailureCode);
    }

    private static bool TryParseDate(string? value, out DateOnly date)
        =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
}
