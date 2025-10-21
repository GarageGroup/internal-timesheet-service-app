using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetModifyFunc
{
    public ValueTask<Result<Unit, Failure<TimesheetUpdateFailureCode>>> InvokeAsync(
        TimesheetUpdateIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .PipeValue(
            BuildTimesheetJsonOrFailureAsync)
        .MapSuccess(
            timesheet => TimesheetJson.BuildDataverseUpdateInput(input.TimesheetId, timesheet))
        .ForwardValue(
            dataverseApi.UpdateEntityAsync,
            static failure => failure.MapFailureCode(ToTimesheetUpdateFailureCode));

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