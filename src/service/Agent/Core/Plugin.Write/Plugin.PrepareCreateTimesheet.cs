using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

partial class AgentWritePlugin
{
    [KernelFunction("prepare_create_timesheet")]
    [Description("Prepares one time entry for explicit user confirmation. Does not create or modify a Dataverse record.")]
    public async Task<AgentWriteToolResult<AgentTimesheetCreatePrepareOut>> PrepareCreateTimesheetAsync(
        [Description("Date of the time entry in YYYY-MM-DD format.")] string date,
        [Description("Project identifier returned by search_projects or get_recent_projects.")] Guid projectId,
        [Description("Project name returned together with the selected project identifier.")] string projectName,
        [Description("Numeric project type returned together with the selected project identifier.")] int projectType,
        [Description("Duration in decimal hours. Must be greater than zero.")] decimal duration,
        [Description("Required time entry description.")] string description,
        CancellationToken cancellationToken)
    {
        if (actionCapture.TryStart() is false)
        {
            return BuildFailure<AgentTimesheetCreatePrepareOut, AgentWriteToolFailureCode>(
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

            return BuildFailure<AgentTimesheetCreatePrepareOut, AgentWriteToolFailureCode>(
                AgentWriteToolFailureCode.InvalidDateFormat);
        }

        var result = await timesheetCreatePrepareFunc.InvokeAsync(
            context,
            new(parsedDate, projectId, projectName, (ProjectType)projectType, duration, description),
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            actionCapture.Reset();

            return BuildFailure<AgentTimesheetCreatePrepareOut, AgentTimesheetCreatePrepareFailureCode>(
                result.FailureOrThrow().FailureCode);
        }

        var action = result.SuccessOrThrow();
        actionCapture.Complete(action);

        return BuildSuccess(action);
    }
}
