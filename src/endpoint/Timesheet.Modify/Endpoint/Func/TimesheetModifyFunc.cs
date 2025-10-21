using GarageGroup.Infra;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class TimesheetModifyFunc(IDataverseApiClient dataverseApi) : ITimesheetCreateFunc, ITimesheetUpdateFunc
{
    private const int TelegramChannelCode = 140120000;

    private ValueTask<Result<IProjectJson, Failure<ProjectNameFailureCode>>> GetProjectAsync(
        TimesheetProject input, Guid systemUserId, CancellationToken cancellationToken)
        =>
        input.Type switch
        {
            ProjectType.Project => InnerGetProjectAsync<ProjectJson>(input.Id, systemUserId, cancellationToken),
            ProjectType.Incident => InnerGetProjectAsync<IncidentJson>(input.Id, systemUserId, cancellationToken),
            ProjectType.Opportunity => InnerGetProjectAsync<OpportunityJson>(input.Id, systemUserId, cancellationToken),
            ProjectType.Lead => InnerGetProjectAsync<LeadJson>(input.Id, systemUserId, cancellationToken),
            _ => new(Failure.Create(ProjectNameFailureCode.InvalidProject, $"An unexpected project type: {input.Type}"))
        };

    private ValueTask<Result<IProjectJson, Failure<ProjectNameFailureCode>>> InnerGetProjectAsync<TProjectJson>(
        Guid projectId, Guid systemUserId, CancellationToken cancellationToken)
        where TProjectJson : IProjectJson, IProjectDataverseInputBuilder, new()
        =>
        AsyncPipeline.Pipe(
            TProjectJson.BuildDataverseEntityGetIn(projectId, systemUserId), cancellationToken)
        .PipeValue(
            dataverseApi.GetEntityAsync<TProjectJson>)
        .Map(
            static @out => (IProjectJson)(@out.Value ?? new()),
            static failure => failure.MapFailureCode(ToProjectNameFailureCode));

    private static ProjectNameFailureCode ToProjectNameFailureCode(DataverseFailureCode failureCode)
        =>
        failureCode switch
        {
            DataverseFailureCode.RecordNotFound => ProjectNameFailureCode.ProjectNotFound,
            _ => default
        };

    private enum ProjectNameFailureCode
    {
        Unknown,

        ProjectNotFound,

        InvalidProject
    }
}