using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetModifyApi
{
    public ValueTask<Result<Unit, Failure<TimesheetUpdateFailureCode>>> UpdateAsync(
        TimesheetUpdateIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .PipeValue(
            ValidateDateAsync)
        .ForwardValue(
            BuildTimesheetJsonOrFailureAsync)
        .MapSuccess(
            timesheet => TimesheetJson.BuildDataverseUpdateInput(
                timesheetId: input.TimesheetId,
                timesheet: timesheet,
                callerObjectId: input.SystemUserId))
        .ForwardValue(
            dataverseApi.UpdateEntityAsync,
            static failure => failure.MapFailureCode(ToTimesheetUpdateFailureCode));

    // The entry is read first: its current date may be in a closed period even if the date is not changed
    private async ValueTask<Result<TimesheetUpdateIn, Failure<TimesheetUpdateFailureCode>>> ValidateDateAsync(
        TimesheetUpdateIn input, CancellationToken cancellationToken)
    {
        var timesheet = await dataverseApi.GetEntityAsync<TimesheetDateJson>(
            TimesheetDateJson.BuildDataverseGetInput(input.TimesheetId, input.SystemUserId), cancellationToken).ConfigureAwait(false);

        if (timesheet.IsFailure)
        {
            return timesheet.FailureOrThrow().MapFailureCode(ToTimesheetUpdateFailureCode);
        }

        var result = await ValidateDateAsync(
            input, newDate: input.Date, existingDate: timesheet.SuccessOrThrow().Value?.Date, cancellationToken).ConfigureAwait(false);

        return result.MapFailure(static failure => failure.MapFailureCode(ToTimesheetUpdateFailureCode));
    }

    private async ValueTask<Result<TimesheetJson, Failure<TimesheetUpdateFailureCode>>> BuildTimesheetJsonOrFailureAsync(
        TimesheetUpdateIn input, CancellationToken cancellationToken)
    {
        if (input.Project is null)
        {
            return new TimesheetJson
            {
                Date = input.Date,
                Description = input.Description,
                Duration = input.Duration
            };
        }

        var projectName = await GetProjectAsync(input.Project, input.SystemUserId, cancellationToken).ConfigureAwait(false);
        return projectName.Map(MapTimesheetJson, MapFailure);

        TimesheetJson MapTimesheetJson(IProjectJson project)
            =>
            new(project)
            {
                Date = input.Date,
                Description = input.Description,
                Duration = input.Duration
            };

        static Failure<TimesheetUpdateFailureCode> MapFailure(Failure<ProjectNameFailureCode> failure)
            =>
            failure.MapFailureCode(ToTimesheetUpdateFailureCode);
    }

    private static TimesheetUpdateFailureCode ToTimesheetUpdateFailureCode(DataverseFailureCode failureCode)
        =>
        failureCode switch
        {
            DataverseFailureCode.RecordNotFound => TimesheetUpdateFailureCode.TimesheetNotFound,
            DataverseFailureCode.CannotUpdateBecauseItIsReadOnly => TimesheetUpdateFailureCode.BadRequest,
            DataverseFailureCode.IsvAborted => TimesheetUpdateFailureCode.BadRequest,
            _ => default
        };

    private static TimesheetUpdateFailureCode ToTimesheetUpdateFailureCode(DateFailureCode failureCode)
        =>
        failureCode switch
        {
            DateFailureCode.FutureDate => TimesheetUpdateFailureCode.FutureDate,
            DateFailureCode.PeriodClosed => TimesheetUpdateFailureCode.PeriodClosed,
            _ => default
        };

    private static TimesheetUpdateFailureCode ToTimesheetUpdateFailureCode(ProjectNameFailureCode failureCode)
        =>
        failureCode switch
        {
            ProjectNameFailureCode.ProjectNotFound => TimesheetUpdateFailureCode.ProjectNotFound,
            ProjectNameFailureCode.InvalidProject => TimesheetUpdateFailureCode.UnexpectedProjectType,
            _ => default
        };
}
