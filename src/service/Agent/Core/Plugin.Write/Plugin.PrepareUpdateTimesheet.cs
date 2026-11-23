using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

partial class AgentWritePlugin
{
    [KernelFunction("prepare_update_timesheet")]
    [Description("Prepares changes to one existing time entry for explicit user confirmation. Does not modify Dataverse.")]
    public async Task<AgentWriteToolResult<AgentTimesheetUpdatePrepareOut>> PrepareUpdateTimesheetAsync(
        [Description("Identifier returned by get_timesheets.")] Guid timesheetId,
        [Description("Current date returned with the selected time entry in YYYY-MM-DD format.")] string sourceDate,
        [Description("Optional new date in YYYY-MM-DD format. Pass null to keep the current date.")] string? date,
        [Description("Optional new project identifier returned by search_projects or get_recent_projects. Pass null to keep the current project.")] Guid? projectId,
        [Description("Optional new duration in decimal hours. Pass null to keep the current duration.")] decimal? duration,
        [Description("Optional new non-empty description. Pass null to keep the current description.")] string? description,
        CancellationToken cancellationToken)
    {
        if (actionCapture.TryStart() is false)
        {
            return BuildFailure<AgentTimesheetUpdatePrepareOut, AgentWriteToolFailureCode>(
                AgentWriteToolFailureCode.ActionAlreadyPrepared);
        }

        if (TryParseDate(sourceDate, out var parsedSourceDate) is false ||
            TryParseOptionalDate(date, out var parsedDate) is false)
        {
            actionCapture.Reset();

            return BuildFailure<AgentTimesheetUpdatePrepareOut, AgentWriteToolFailureCode>(
                AgentWriteToolFailureCode.InvalidDateFormat);
        }

        var result = await timesheetUpdatePrepareFunc.InvokeAsync(
            context,
            new(timesheetId, parsedSourceDate, parsedDate, projectId, duration, description),
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            actionCapture.Reset();

            return BuildFailure<AgentTimesheetUpdatePrepareOut, AgentTimesheetUpdatePrepareFailureCode>(
                result.FailureOrThrow().FailureCode);
        }

        var action = result.SuccessOrThrow();
        actionCapture.Complete(action);

        return BuildSuccess(action);
    }

    private static bool TryParseDate(string value, out DateOnly date)
        =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    private static bool TryParseOptionalDate(string? value, out DateOnly? date)
    {
        if (value is null)
        {
            date = null;

            return true;
        }

        var isValid = TryParseDate(value, out var parsedDate);
        date = isValid ? parsedDate : null;

        return isValid;
    }
}
