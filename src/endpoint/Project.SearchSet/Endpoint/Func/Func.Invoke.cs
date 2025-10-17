using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class ProjectSetSearchFunc
{
    public ValueTask<Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>> InvokeAsync(
        ProjectSetSearchIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            static @in => new DataverseSearchIn($"*{@in.SearchText}*")
            {
                Top = @in.Top,
                Entities = EntityNames,
                Filter = IsActiveFilter,
                CallerObjectId = @in.SystemUserId
            })
        .PipeValue(
            dataverseApi.SearchAsync)
        .Map(
            static success => new ProjectSetSearchOut
            {
                Projects = GetProjects(success.Value).ToFlatArray()
            },
            static failure => failure.MapFailureCode(MapFailureCode));

    private static IEnumerable<ProjectItem> GetProjects(FlatArray<DataverseSearchItem> items)
    {
        foreach (var item in items)
        {
            var projectType = GetProjectType(item);
            if (projectType is null)
            {
                continue;
            }

            yield return new(
                id: item.ObjectId,
                name: GetProjectName(item, projectType.Value),
                type: projectType.Value);
        }
    }

    private static ProjectSetSearchFailureCode MapFailureCode(DataverseFailureCode failureCode)
        =>
        failureCode switch
        {
            DataverseFailureCode.UserNotEnabled => ProjectSetSearchFailureCode.Forbidden,
            DataverseFailureCode.PrivilegeDenied => ProjectSetSearchFailureCode.Forbidden,
            DataverseFailureCode.SearchableEntityNotFound => ProjectSetSearchFailureCode.Forbidden,
            _ => default
        };
}