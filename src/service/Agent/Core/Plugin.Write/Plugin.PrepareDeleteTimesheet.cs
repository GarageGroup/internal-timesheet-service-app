using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

partial class AgentWritePlugin
{
    [KernelFunction("prepare_delete_timesheet")]
    [Description("Prepares one existing time entry for explicit user confirmation. Does not delete or modify a Dataverse record.")]
    public async Task<AgentWriteToolResult<AgentTimesheetDeletePrepareOut>> PrepareDeleteTimesheetAsync(
        [Description("Time entry identifier returned by get_timesheets.")] Guid timesheetId,
        [Description("Date of the time entry returned by get_timesheets in YYYY-MM-DD format.")] string date,
        CancellationToken cancellationToken)
    {
        if (actionCapture.TryStart() is false)
        {
            return BuildFailure<AgentTimesheetDeletePrepareOut, AgentWriteToolFailureCode>(
                AgentWriteToolFailureCode.ActionAlreadyPrepared);
        }

        if (DateOnly.TryParseExact(
            date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDate) is false)
        {
            actionCapture.Reset();

            return BuildFailure<AgentTimesheetDeletePrepareOut, AgentWriteToolFailureCode>(
                AgentWriteToolFailureCode.InvalidDateFormat);
        }

        var result = await timesheetDeletePrepareFunc.InvokeAsync(
            context,
            new(timesheetId, parsedDate),
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            actionCapture.Reset();

            return BuildFailure<AgentTimesheetDeletePrepareOut, AgentTimesheetDeletePrepareFailureCode>(
                result.FailureOrThrow().FailureCode);
        }

        var action = result.SuccessOrThrow();
        actionCapture.Complete(action);

        return BuildSuccess(action);
    }
}
